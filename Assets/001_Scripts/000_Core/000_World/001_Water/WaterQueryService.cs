using System.Collections.Generic;
using System.Runtime.CompilerServices;
using _001_Scripts.Core._000_World._001_Water.Interface;
using UnityEngine;

namespace _001_Scripts.Core._000_World._001_Water
{
    [AddComponentMenu("Survival/Water/Water Query Service")]
    public sealed class WaterQueryService : MonoBehaviour, IWaterQueryService, IWaterQuery, IWaterRegistry
    {
        private const int MaximumCellsPerBody = 4096;
        private const int PurgeInterval = 256;

        [Min(8f), SerializeField] private float spatialCellSize = 64f;
        [SerializeField] private OceanBody _oceanBody;

        private readonly List<IWaterBody> _bodies = new List<IWaterBody>(16);
        private readonly List<IWaterBody> _globalBodies = new List<IWaterBody>(2);
        private readonly Dictionary<long, List<IWaterBody>> _cells = new Dictionary<long, List<IWaterBody>>();
        private readonly Dictionary<IWaterBody, long[]> _bodyCells =
            new Dictionary<IWaterBody, long[]>(ReferenceComparer.Instance);
        private readonly Dictionary<IWaterBody, int> _registrationOrder =
            new Dictionary<IWaterBody, int>(ReferenceComparer.Instance);

        private int _nextRegistrationOrder;
        private int _samplesUntilPurge = PurgeInterval;

        public int RegisteredBodyCount => _bodies.Count;
        public int IndexedCellCount => _cells.Count;
        public int LastBroadPhaseCandidateCount { get; private set; }
        public int LastSampledBodyCount { get; private set; }

        private void OnEnable()
        {
            spatialCellSize = Mathf.Max(8f, spatialCellSize);
            WaterRegistryLocator.Set(this);
            if (_oceanBody != null) Register((IWaterBody)_oceanBody);
        }

        private void OnDisable()
        {
            WaterRegistryLocator.Clear(this);
            _bodies.Clear();
            _globalBodies.Clear();
            _cells.Clear();
            _bodyCells.Clear();
            _registrationOrder.Clear();
            _nextRegistrationOrder = 0;
            _samplesUntilPurge = PurgeInterval;
        }

        public bool Register(IWaterBody waterBody)
        {
            if (!IsAlive(waterBody) || _registrationOrder.ContainsKey(waterBody)) return false;
            _bodies.Add(waterBody);
            _registrationOrder.Add(waterBody, _nextRegistrationOrder++);
            IndexBody(waterBody);
            return true;
        }

        public bool Unregister(IWaterBody waterBody)
        {
            int index = FindReference(_bodies, waterBody);
            if (index < 0) return false;
            RemoveFromIndex(waterBody);
            _bodies.RemoveAt(index);
            _registrationOrder.Remove(waterBody);
            return true;
        }

        public void Refresh(IWaterBody waterBody)
        {
            if (!_registrationOrder.ContainsKey(waterBody)) return;
            RemoveFromIndex(waterBody);
            if (IsAlive(waterBody)) IndexBody(waterBody);
        }

        public bool TrySample(Vector3 worldPosition, out WaterSample sample)
        {
            MaybePurgeDestroyedBodies();
            LastBroadPhaseCandidateCount = 0;
            LastSampledBodyCount = 0;

            bool found = false;
            WaterSample best = default;
            int bestRegistrationOrder = int.MaxValue;

            SampleCandidates(_globalBodies, worldPosition, ref found, ref best, ref bestRegistrationOrder);
            if (_cells.TryGetValue(CellKey(worldPosition.x, worldPosition.z), out List<IWaterBody> candidates))
                SampleCandidates(candidates, worldPosition, ref found, ref best, ref bestRegistrationOrder);

            sample = best;
            return found;
        }

        public bool TryGetWaterBody(Vector3 worldPosition, out IWaterBody waterBody)
        {
            if (TrySample(worldPosition, out WaterSample sample))
            {
                waterBody = sample.WaterBody;
                return true;
            }

            waterBody = null;
            return false;
        }

        private void SampleCandidates(List<IWaterBody> candidates, Vector3 position,
            ref bool found, ref WaterSample best, ref int bestRegistrationOrder)
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                IWaterBody body = candidates[i];
                if (!IsAlive(body) || !PassesBroadPhase(body, position)) continue;
                LastBroadPhaseCandidateCount++;
                if (!body.TrySample(position, out WaterSample candidate)) continue;
                LastSampledBodyCount++;

                int order = _registrationOrder.TryGetValue(body, out int value) ? value : int.MaxValue;
                if (!found || IsBetter(candidate, order, best, bestRegistrationOrder))
                {
                    found = true;
                    best = candidate;
                    bestRegistrationOrder = order;
                }
            }
        }

        private void IndexBody(IWaterBody body)
        {
            if (body is OceanBody ocean && ocean.IsInfinite)
            {
                _globalBodies.Add(body);
                return;
            }

            Bounds bounds = body.WorldBounds;
            if (!IsFinite(bounds.min) || !IsFinite(bounds.max))
            {
                _globalBodies.Add(body);
                return;
            }

            int minX = CellCoordinate(bounds.min.x);
            int minZ = CellCoordinate(bounds.min.z);
            int maxX = CellCoordinate(bounds.max.x);
            int maxZ = CellCoordinate(bounds.max.z);
            long count = (maxX - (long)minX + 1L) * (maxZ - (long)minZ + 1L);
            if (count <= 0L || count > MaximumCellsPerBody)
            {
                _globalBodies.Add(body);
                return;
            }

            long[] keys = new long[(int)count];
            int write = 0;
            for (int x = minX; x <= maxX; x++)
            for (int z = minZ; z <= maxZ; z++)
            {
                long key = Pack(x, z);
                keys[write++] = key;
                if (!_cells.TryGetValue(key, out List<IWaterBody> values))
                {
                    values = new List<IWaterBody>(2);
                    _cells.Add(key, values);
                }
                values.Add(body);
            }
            _bodyCells.Add(body, keys);
        }

        private void RemoveFromIndex(IWaterBody body)
        {
            int globalIndex = FindReference(_globalBodies, body);
            if (globalIndex >= 0) _globalBodies.RemoveAt(globalIndex);
            if (!_bodyCells.TryGetValue(body, out long[] keys)) return;
            for (int i = 0; i < keys.Length; i++)
            {
                if (!_cells.TryGetValue(keys[i], out List<IWaterBody> values)) continue;
                int index = FindReference(values, body);
                if (index >= 0) values.RemoveAt(index);
                if (values.Count == 0) _cells.Remove(keys[i]);
            }
            _bodyCells.Remove(body);
        }

        private void RebuildSpatialIndex()
        {
            _globalBodies.Clear();
            _cells.Clear();
            _bodyCells.Clear();
            for (int i = 0; i < _bodies.Count; i++)
                if (IsAlive(_bodies[i])) IndexBody(_bodies[i]);
        }

        private void MaybePurgeDestroyedBodies()
        {
            if (--_samplesUntilPurge > 0) return;
            _samplesUntilPurge = PurgeInterval;
            for (int i = _bodies.Count - 1; i >= 0; i--)
            {
                IWaterBody body = _bodies[i];
                if (IsAlive(body)) continue;
                RemoveFromIndex(body);
                _bodies.RemoveAt(i);
                _registrationOrder.Remove(body);
            }
        }

        private int CellCoordinate(float value) => Mathf.FloorToInt(value / spatialCellSize);
        private long CellKey(float x, float z) => Pack(CellCoordinate(x), CellCoordinate(z));
        private static long Pack(int x, int z) => ((long)x << 32) ^ (uint)z;

        private static int FindReference(List<IWaterBody> values, IWaterBody body)
        {
            for (int i = 0; i < values.Count; i++)
                if (ReferenceEquals(values[i], body)) return i;
            return -1;
        }

        private static bool IsBetter(WaterSample candidate, int candidateOrder,
            WaterSample current, int currentOrder)
        {
            int candidatePriority = candidate.WaterBody.Priority;
            int currentPriority = current.WaterBody.Priority;
            if (candidatePriority != currentPriority) return candidatePriority > currentPriority;

            bool candidateLocal = candidate.BodyType != WaterBodyType.Ocean;
            bool currentLocal = current.BodyType != WaterBodyType.Ocean;
            if (candidateLocal != currentLocal) return candidateLocal;

            if (!Mathf.Approximately(candidate.SignedDepth, current.SignedDepth))
                return candidate.SignedDepth > current.SignedDepth;
            return candidateOrder < currentOrder;
        }

        private static bool PassesBroadPhase(IWaterBody body, Vector3 position)
        {
            if (body is OceanBody ocean && ocean.IsInfinite) return true;
            return body.WorldBounds.Contains(position);
        }

        private static bool IsFinite(Vector3 value) =>
            !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
            !float.IsNaN(value.y) && !float.IsInfinity(value.y) &&
            !float.IsNaN(value.z) && !float.IsInfinity(value.z);

        private static bool IsAlive(IWaterBody body)
        {
            if (body == null) return false;
            return !(body is UnityEngine.Object unityObject) || unityObject != null;
        }

        [System.Obsolete("Use Register(IWaterBody).")]
        public void Register(IWaterbody waterbody) => Register((IWaterBody)waterbody);

        [System.Obsolete("Use Unregister(IWaterBody).")]
        public void UnRegister(IWaterbody waterbody) => Unregister(waterbody);

        [System.Obsolete("Use TrySample and inspect SignedDepth.")]
        public bool IsInWater(Vector3 position) =>
            TrySample(position, out WaterSample sample) && sample.IsSubmerged;

        [System.Obsolete("Use TrySample. Returns 0 when no water exists.")]
        public float GetSurfaceY(Vector3 position) =>
            TrySample(position, out WaterSample sample) ? sample.SurfaceHeight : 0f;

        [System.Obsolete("Use TryGetWaterBody. Returns null when no water exists.")]
        public IWaterbody GetWaterBody(Vector3 position) =>
            TryGetWaterBody(position, out IWaterBody body) ? body as IWaterbody : null;

#if UNITY_EDITOR
        private void OnValidate()
        {
            spatialCellSize = Mathf.Max(8f, spatialCellSize);
            if (isActiveAndEnabled) RebuildSpatialIndex();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 0.8f, 1f, 0.25f);
            for (int i = 0; i < _bodies.Count; i++)
            {
                IWaterBody body = _bodies[i];
                if (!IsAlive(body) || body is OceanBody ocean && ocean.IsInfinite) continue;
                Gizmos.DrawWireCube(body.WorldBounds.center, body.WorldBounds.size);
            }
        }
#endif

        private sealed class ReferenceComparer : IEqualityComparer<IWaterBody>
        {
            public static readonly ReferenceComparer Instance = new ReferenceComparer();
            public bool Equals(IWaterBody left, IWaterBody right) => ReferenceEquals(left, right);
            public int GetHashCode(IWaterBody value) => RuntimeHelpers.GetHashCode(value);
        }
    }
}
