using System;
using System.Collections.Generic;
using _001_Scripts.Core._000_World._001_Water.Interface;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Splines;

namespace _001_Scripts.Core._000_World._001_Water
{
    [Serializable]
    public struct RiverParameterKey
    {
        [Range(0f, 1f)] public float normalizedPosition;
        [Min(0.1f)] public float width;
        [Min(0.1f)] public float depth;
        [Min(0f)] public float flowSpeed;
        public float surfaceOffset;
        [Range(0f, 1f)] public float bankSoftness;
    }

    [RequireComponent(typeof(SplineContainer), typeof(MeshFilter), typeof(MeshRenderer))]
    [AddComponentMenu("Survival/Water/Spline River Water Body")]
    public sealed class SplineRiverWaterBody : WaterBodyBehaviour, IWaterbody
    {
        private const float MinimumVectorLength = 0.000001f;

        [Header("Source")]
        [SerializeField] private SplineContainer splineContainer;
        [SerializeField] private RiverProfile profile;
        [SerializeField] private Material materialOverride;

        [Header("Fallback Settings")]
        [Min(0.1f), SerializeField] private float width = 8f;
        [Min(0.1f), SerializeField] private float depth = 3f;
        [Min(0f), SerializeField] private float flowSpeed = 2f;
        [Min(0.1f), SerializeField] private float sampleSpacing = 1f;
        [Min(0.001f), SerializeField] private float uvScale = 0.1f;
        [Min(0f), SerializeField] private float sampleHeightAboveSurface = 2f;
        [SerializeField] private bool reverseFlow;
        [SerializeField] private float flowMultiplier = 1f;
        [SerializeField] private bool fadeEnds = true;
        [Min(0f), SerializeField] private float fadeDistance = 3f;
        [SerializeField] private bool generateSidesAndBottom;
        [SerializeField] private bool generateCollider;
        [SerializeField] private bool autoRebuild = true;
        [Header("Query Performance")]
        [Min(2f), SerializeField] private float queryCellSize = 16f;
        [SerializeField] private RiverParameterKey[] parameterKeys = Array.Empty<RiverParameterKey>();

        [NonSerialized] private RiverSample[] _samples = Array.Empty<RiverSample>();
        [NonSerialized] private Mesh _generatedMesh;
        [NonSerialized] private Bounds _worldBounds;
        [NonSerialized] private bool _cacheValid;
        [NonSerialized] private MaterialPropertyBlock _propertyBlock;
        [NonSerialized] private Dictionary<long, int[]> _segmentCells = new Dictionary<long, int[]>();

        private struct RiverSample
        {
            public Vector3 center;
            public Vector3 tangent;
            public Vector3 up;
            public Vector3 right;
            public float width;
            public float depth;
            public float flowSpeed;
            public float distance;
            public float normalizedPosition;
        }

        public override Bounds WorldBounds
        {
            get
            {
                EnsureCache();
                return _worldBounds;
            }
        }

        public RiverProfile Profile => profile;
        public bool AutoRebuild => autoRebuild;
        public int SampleCount => _samples.Length;
        public int LastQueryCandidateCount { get; private set; }
        public Mesh GeneratedMesh => _generatedMesh;

        protected override void OnEnable()
        {
            base.OnEnable();
            Spline.Changed += OnSplineChanged;
            EnsureCache();
            ApplyMaterial();
        }

        protected override void OnDisable()
        {
            Spline.Changed -= OnSplineChanged;
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            Spline.Changed -= OnSplineChanged;
            ClearGeneratedMesh();
            base.OnDestroy();
        }

        public override bool TrySample(Vector3 worldPosition, out WaterSample sample)
        {
            EnsureCache();
            if (_samples.Length < 2 || !_worldBounds.Contains(worldPosition))
            {
                sample = default;
                return false;
            }

            Vector2 query = new Vector2(worldPosition.x, worldPosition.z);
            float bestDistanceSquared = float.PositiveInfinity;
            int bestSegment = -1;
            float bestT = 0f;
            if (!_segmentCells.TryGetValue(SegmentCellKey(worldPosition.x, worldPosition.z), out int[] candidates))
            {
                LastQueryCandidateCount = 0;
                sample = default;
                return false;
            }

            LastQueryCandidateCount = candidates.Length;
            for (int candidateIndex = 0; candidateIndex < candidates.Length; candidateIndex++)
            {
                int i = candidates[candidateIndex];
                Vector2 start = new Vector2(_samples[i].center.x, _samples[i].center.z);
                Vector2 end = new Vector2(_samples[i + 1].center.x, _samples[i + 1].center.z);
                Vector2 delta = end - start;
                float lengthSquared = delta.sqrMagnitude;
                float t = lengthSquared > MinimumVectorLength
                    ? Mathf.Clamp01(Vector2.Dot(query - start, delta) / lengthSquared)
                    : 0f;
                float distanceSquared = (query - Vector2.Lerp(start, end, t)).sqrMagnitude;
                if (distanceSquared >= bestDistanceSquared) continue;
                bestDistanceSquared = distanceSquared;
                bestSegment = i;
                bestT = t;
            }

            if (bestSegment < 0)
            {
                sample = default;
                return false;
            }

            RiverSample a = _samples[bestSegment];
            RiverSample b = _samples[bestSegment + 1];
            float localWidth = Mathf.Lerp(a.width, b.width, bestT);
            if (bestDistanceSquared > localWidth * localWidth * 0.25f)
            {
                sample = default;
                return false;
            }

            Vector3 center = Vector3.Lerp(a.center, b.center, bestT);
            float localDepth = Mathf.Lerp(a.depth, b.depth, bestT);
            if (worldPosition.y < center.y - localDepth || worldPosition.y > center.y + sampleHeightAboveSurface)
            {
                sample = default;
                return false;
            }

            Vector3 tangent = Vector3.Slerp(a.tangent, b.tangent, bestT).normalized;
            Vector3 up = Vector3.Slerp(a.up, b.up, bestT).normalized;
            float speed = Mathf.Lerp(a.flowSpeed, b.flowSpeed, bestT) * flowMultiplier;
            Vector3 velocity = tangent * (reverseFlow ? -speed : speed);
            sample = new WaterSample(this, worldPosition,
                new Vector3(worldPosition.x, center.y, worldPosition.z), up, velocity, WaterBodyType.River);
            return true;
        }

        public bool TryGetNearestCenterline(Vector3 worldPosition, out Vector3 center,
            out float localWidth, out float localDepth, out float lateralDistance)
        {
            EnsureCache();
            center = default;
            localWidth = 0f;
            localDepth = 0f;
            lateralDistance = float.PositiveInfinity;
            if (_samples.Length < 2) return false;

            Vector2 query = new Vector2(worldPosition.x, worldPosition.z);
            for (int i = 0; i < _samples.Length - 1; i++)
            {
                RiverSample a = _samples[i];
                RiverSample b = _samples[i + 1];
                Vector2 start = new Vector2(a.center.x, a.center.z);
                Vector2 end = new Vector2(b.center.x, b.center.z);
                Vector2 delta = end - start;
                float lengthSquared = delta.sqrMagnitude;
                float t = lengthSquared > MinimumVectorLength
                    ? Mathf.Clamp01(Vector2.Dot(query - start, delta) / lengthSquared)
                    : 0f;
                float distance = Vector2.Distance(query, Vector2.Lerp(start, end, t));
                if (distance >= lateralDistance) continue;
                lateralDistance = distance;
                center = Vector3.Lerp(a.center, b.center, t);
                localWidth = Mathf.Lerp(a.width, b.width, t);
                localDepth = Mathf.Lerp(a.depth, b.depth, t);
            }

            return lateralDistance < float.PositiveInfinity;
        }

        public void Rebuild()
        {
            RebuildCache();
            RebuildMesh();
            ApplyMaterial();
            NotifyBoundsChanged();
        }

        public void ClearGeneratedMesh()
        {
            MeshFilter filter = GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh == _generatedMesh) filter.sharedMesh = null;
            if (_generatedMesh == null) return;

            if (Application.isPlaying) Destroy(_generatedMesh);
            else DestroyImmediate(_generatedMesh);
            _generatedMesh = null;
        }

        private void EnsureCache()
        {
            if (_cacheValid) return;
            RebuildCache();
        }

        private void RebuildCache()
        {
            _cacheValid = true;
            if (splineContainer == null) splineContainer = GetComponent<SplineContainer>();
            if (splineContainer == null || splineContainer.Splines.Count == 0 || splineContainer.Splines[0].Count < 2)
            {
                _samples = Array.Empty<RiverSample>();
                _segmentCells.Clear();
                _worldBounds = new Bounds(transform.position, Vector3.zero);
                return;
            }

            float length = Mathf.Max(0.001f, splineContainer.CalculateLength());
            float spacing = Mathf.Max(0.1f, ProfileSpacing);
            int count = Mathf.Max(2, Mathf.CeilToInt(length / spacing) + 1);
            _samples = new RiverSample[count];
            Bounds bounds = default;
            Vector3 previousCenter = Vector3.zero;
            Vector3 previousRight = Vector3.right;
            float accumulatedDistance = 0f;

            for (int i = 0; i < count; i++)
            {
                float t = i / (count - 1f);
                splineContainer.Evaluate(t, out float3 position, out float3 tangentValue, out float3 upValue);
                EvaluateParameters(t, out float sampleWidth, out float sampleDepth,
                    out float sampleFlow, out float surfaceOffset);

                Vector3 tangent = ((Vector3)tangentValue).normalized;
                if (tangent.sqrMagnitude < MinimumVectorLength) tangent = i > 0 ? _samples[i - 1].tangent : Vector3.forward;
                Vector3 up = ((Vector3)upValue).normalized;
                if (up.sqrMagnitude < MinimumVectorLength || Mathf.Abs(Vector3.Dot(up, tangent)) > 0.995f)
                    up = Mathf.Abs(Vector3.Dot(Vector3.up, tangent)) < 0.995f ? Vector3.up : Vector3.forward;
                Vector3 right = Vector3.Cross(up, tangent).normalized;
                if (right.sqrMagnitude < MinimumVectorLength)
                    right = Vector3.Cross(Vector3.forward, tangent).normalized;
                if (i > 0 && Vector3.Dot(previousRight, right) < 0f) right = -right;
                up = Vector3.Cross(tangent, right).normalized;

                Vector3 center = (Vector3)position + up * surfaceOffset;
                if (i > 0) accumulatedDistance += Vector3.Distance(previousCenter, center);
                _samples[i] = new RiverSample
                {
                    center = center,
                    tangent = tangent,
                    up = up,
                    right = right,
                    width = sampleWidth,
                    depth = sampleDepth,
                    flowSpeed = sampleFlow,
                    distance = accumulatedDistance,
                    normalizedPosition = t
                };

                Vector3 left = center - right * sampleWidth * 0.5f;
                Vector3 rightEdge = center + right * sampleWidth * 0.5f;
                Vector3 bottom = center - Vector3.up * sampleDepth;
                if (i == 0) bounds = new Bounds(center, Vector3.zero);
                bounds.Encapsulate(left);
                bounds.Encapsulate(rightEdge);
                bounds.Encapsulate(bottom);
                bounds.Encapsulate(center + Vector3.up * sampleHeightAboveSurface);
                previousCenter = center;
                previousRight = right;
            }

            _worldBounds = bounds;
            BuildSegmentIndex();
        }

        private void BuildSegmentIndex()
        {
            queryCellSize = Mathf.Max(2f, queryCellSize);
            Dictionary<long, List<int>> builders = new Dictionary<long, List<int>>();
            for (int i = 0; i < _samples.Length - 1; i++)
            {
                RiverSample a = _samples[i];
                RiverSample b = _samples[i + 1];
                float halfWidth = Mathf.Max(a.width, b.width) * 0.5f;
                float minX = Mathf.Min(a.center.x, b.center.x) - halfWidth;
                float maxX = Mathf.Max(a.center.x, b.center.x) + halfWidth;
                float minZ = Mathf.Min(a.center.z, b.center.z) - halfWidth;
                float maxZ = Mathf.Max(a.center.z, b.center.z) + halfWidth;
                int cellMinX = SegmentCellCoordinate(minX);
                int cellMaxX = SegmentCellCoordinate(maxX);
                int cellMinZ = SegmentCellCoordinate(minZ);
                int cellMaxZ = SegmentCellCoordinate(maxZ);
                for (int x = cellMinX; x <= cellMaxX; x++)
                for (int z = cellMinZ; z <= cellMaxZ; z++)
                {
                    long key = PackCell(x, z);
                    if (!builders.TryGetValue(key, out List<int> values))
                    {
                        values = new List<int>(4);
                        builders.Add(key, values);
                    }
                    values.Add(i);
                }
            }

            _segmentCells = new Dictionary<long, int[]>(builders.Count);
            foreach (KeyValuePair<long, List<int>> pair in builders)
                _segmentCells.Add(pair.Key, pair.Value.ToArray());
        }

        private int SegmentCellCoordinate(float value) => Mathf.FloorToInt(value / queryCellSize);
        private long SegmentCellKey(float x, float z) => PackCell(SegmentCellCoordinate(x), SegmentCellCoordinate(z));
        private static long PackCell(int x, int z) => ((long)x << 32) ^ (uint)z;

        private void RebuildMesh()
        {
            if (_samples.Length < 2) return;
            if (_generatedMesh == null)
            {
                _generatedMesh = new Mesh { name = $"{name}_GeneratedRiver" };
                _generatedMesh.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
            }
            else
            {
                _generatedMesh.Clear();
            }

            int rows = generateSidesAndBottom ? 4 : 2;
            int vertexCount = _samples.Length * rows;
            Vector3[] vertices = new Vector3[vertexCount];
            Vector3[] normals = new Vector3[vertexCount];
            Vector4[] tangents = new Vector4[vertexCount];
            Vector2[] uvs = new Vector2[vertexCount];
            Color[] colors = new Color[vertexCount];

            float totalLength = _samples[_samples.Length - 1].distance;
            for (int i = 0; i < _samples.Length; i++)
            {
                RiverSample riverSample = _samples[i];
                Vector3 left = riverSample.center - riverSample.right * riverSample.width * 0.5f;
                Vector3 right = riverSample.center + riverSample.right * riverSample.width * 0.5f;
                float alpha = GetEndFade(riverSample.distance, totalLength);
                int index = i * rows;
                WriteVertex(index, left, riverSample.up, riverSample.tangent, 0f, riverSample.distance, alpha,
                    vertices, normals, tangents, uvs, colors);
                WriteVertex(index + 1, right, riverSample.up, riverSample.tangent, 1f, riverSample.distance, alpha,
                    vertices, normals, tangents, uvs, colors);

                if (generateSidesAndBottom)
                {
                    Vector3 down = Vector3.down * riverSample.depth;
                    WriteVertex(index + 2, left + down, -riverSample.right, riverSample.tangent, 0f,
                        riverSample.distance, alpha, vertices, normals, tangents, uvs, colors);
                    WriteVertex(index + 3, right + down, riverSample.right, riverSample.tangent, 1f,
                        riverSample.distance, alpha, vertices, normals, tangents, uvs, colors);
                }
            }

            int quadsPerSegment = generateSidesAndBottom ? 4 : 1;
            int[] triangles = new int[(_samples.Length - 1) * quadsPerSegment * 6];
            int triangleIndex = 0;
            for (int i = 0; i < _samples.Length - 1; i++)
            {
                int current = i * rows;
                int next = (i + 1) * rows;
                AddQuad(current, next, next + 1, current + 1, triangles, ref triangleIndex);
                if (!generateSidesAndBottom) continue;
                AddQuad(current + 2, next + 2, next, current, triangles, ref triangleIndex);
                AddQuad(current + 1, next + 1, next + 3, current + 3, triangles, ref triangleIndex);
                AddQuad(current + 3, next + 3, next + 2, current + 2, triangles, ref triangleIndex);
            }

            _generatedMesh.indexFormat = vertexCount > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16;
            _generatedMesh.vertices = vertices;
            _generatedMesh.normals = normals;
            _generatedMesh.tangents = tangents;
            _generatedMesh.uv = uvs;
            _generatedMesh.colors = colors;
            _generatedMesh.triangles = triangles;
            _generatedMesh.RecalculateBounds();
            GetComponent<MeshFilter>().sharedMesh = _generatedMesh;

            MeshCollider collider = GetComponent<MeshCollider>();
            if (generateCollider)
            {
                if (collider == null)
                {
#if UNITY_EDITOR
                    collider = Application.isPlaying
                        ? gameObject.AddComponent<MeshCollider>()
                        : UnityEditor.Undo.AddComponent<MeshCollider>(gameObject);
#else
                    collider = gameObject.AddComponent<MeshCollider>();
#endif
                }
                collider.sharedMesh = null;
                collider.sharedMesh = _generatedMesh;
            }
            else if (collider != null && collider.sharedMesh == _generatedMesh)
            {
                collider.sharedMesh = null;
            }
        }

        private static void AddQuad(int a, int b, int c, int d, int[] triangles, ref int index)
        {
            triangles[index++] = a;
            triangles[index++] = b;
            triangles[index++] = c;
            triangles[index++] = a;
            triangles[index++] = c;
            triangles[index++] = d;
        }

        private void WriteVertex(int index, Vector3 worldPosition, Vector3 normal, Vector3 tangent,
            float u, float distance, float alpha, Vector3[] vertices, Vector3[] normals,
            Vector4[] tangents, Vector2[] uvs, Color[] colors)
        {
            vertices[index] = transform.InverseTransformPoint(worldPosition);
            normals[index] = transform.InverseTransformDirection(normal).normalized;
            Vector3 localTangent = transform.InverseTransformDirection(tangent).normalized;
            tangents[index] = new Vector4(localTangent.x, localTangent.y, localTangent.z, 1f);
            uvs[index] = new Vector2(u, distance * ProfileUvScale);
            colors[index] = new Color(1f, 1f, 1f, alpha);
        }

        private float GetEndFade(float distance, float totalLength)
        {
            if (!fadeEnds || fadeDistance <= 0f) return 1f;
            return Mathf.Clamp01(Mathf.Min(distance, totalLength - distance) / fadeDistance);
        }

        private void EvaluateParameters(float t, out float sampleWidth, out float sampleDepth,
            out float sampleFlow, out float surfaceOffset)
        {
            sampleWidth = ProfileWidth;
            sampleDepth = ProfileDepth;
            sampleFlow = ProfileFlowSpeed;
            surfaceOffset = 0f;
            if (parameterKeys == null || parameterKeys.Length == 0) return;

            RiverParameterKey left = parameterKeys[0];
            RiverParameterKey right = left;
            for (int i = 1; i < parameterKeys.Length; i++)
            {
                right = parameterKeys[i];
                if (right.normalizedPosition >= t) break;
                left = right;
            }

            float range = right.normalizedPosition - left.normalizedPosition;
            float interpolation = range > 0.000001f
                ? Mathf.Clamp01((t - left.normalizedPosition) / range)
                : 0f;
            sampleWidth = Mathf.Lerp(left.width, right.width, interpolation);
            sampleDepth = Mathf.Lerp(left.depth, right.depth, interpolation);
            sampleFlow = Mathf.Lerp(left.flowSpeed, right.flowSpeed, interpolation);
            surfaceOffset = Mathf.Lerp(left.surfaceOffset, right.surfaceOffset, interpolation);
        }

        private void ApplyMaterial()
        {
            MeshRenderer renderer = GetComponent<MeshRenderer>();
            Material material = materialOverride != null ? materialOverride : profile != null ? profile.Material : null;
            if (renderer != null && material != null) renderer.sharedMaterial = material;
            if (renderer == null) return;
            if (_propertyBlock == null) _propertyBlock = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetFloat("_UseWaves", 0f);
            _propertyBlock.SetFloat("_FlowSpeed", ProfileFlowSpeed * flowMultiplier);
            renderer.SetPropertyBlock(_propertyBlock);
        }

        private void OnSplineChanged(Spline changedSpline, int knotIndex, SplineModification modification)
        {
            if (splineContainer == null || splineContainer.Splines.Count == 0 ||
                !ReferenceEquals(splineContainer.Splines[0], changedSpline)) return;
            _cacheValid = false;
            if (autoRebuild && !Application.isPlaying) ScheduleEditorRebuild();
        }

        private float ProfileWidth => profile != null ? profile.Width : width;
        private float ProfileDepth => profile != null ? profile.Depth : depth;
        private float ProfileFlowSpeed => profile != null ? profile.FlowSpeed : flowSpeed;
        private float ProfileSpacing => profile != null ? profile.SampleSpacing : sampleSpacing;
        private float ProfileUvScale => profile != null ? profile.UvScale : uvScale;

        [System.Obsolete("Use TrySample.")]
        public float GetSurfaceY(Vector3 position) =>
            TrySample(position, out WaterSample sample) ? sample.SurfaceHeight : 0f;

        [System.Obsolete("Use TrySample and inspect SignedDepth.")]
        public bool Contain(Vector3 position) =>
            TrySample(position, out WaterSample sample) && sample.IsSubmerged;

#if UNITY_EDITOR
        private bool _editorRebuildQueued;

        private void Reset()
        {
            splineContainer = GetComponent<SplineContainer>();
            ScheduleEditorRebuild();
        }

        private void OnValidate()
        {
            width = Mathf.Max(0.1f, width);
            depth = Mathf.Max(0.1f, depth);
            sampleSpacing = Mathf.Max(0.1f, sampleSpacing);
            queryCellSize = Mathf.Max(2f, queryCellSize);
            uvScale = Mathf.Max(0.001f, uvScale);
            _cacheValid = false;
            if (autoRebuild && !UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                ScheduleEditorRebuild();
        }

        private void ScheduleEditorRebuild()
        {
            if (_editorRebuildQueued || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return;
            _editorRebuildQueued = true;
            UnityEditor.EditorApplication.delayCall += DeferredEditorRebuild;
        }

        private void DeferredEditorRebuild()
        {
            _editorRebuildQueued = false;
            if (this == null || !autoRebuild || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return;
            Rebuild();
        }

        private void OnDrawGizmosSelected()
        {
            if (!ShowGizmos) return;
            EnsureCache();
            int stride = Mathf.Max(1, Mathf.CeilToInt(_samples.Length / 256f));
            for (int i = 0; i < _samples.Length; i += stride)
            {
                RiverSample riverSample = _samples[i];
                Vector3 left = riverSample.center - riverSample.right * riverSample.width * 0.5f;
                Vector3 right = riverSample.center + riverSample.right * riverSample.width * 0.5f;
                Gizmos.color = new Color(0f, 0.7f, 1f, 0.9f);
                Gizmos.DrawLine(left, right);
                Gizmos.DrawSphere(riverSample.center, 0.06f);
                Gizmos.color = new Color(0.1f, 1f, 0.8f, 0.9f);
                Vector3 flow = riverSample.tangent * (reverseFlow ? -1f : 1f);
                Gizmos.DrawLine(riverSample.center, riverSample.center + flow * Mathf.Min(2f, riverSample.flowSpeed));
                Gizmos.color = new Color(0f, 0.4f, 1f, 0.45f);
                Gizmos.DrawLine(riverSample.center, riverSample.center - Vector3.up * riverSample.depth);
            }
        }
#endif
    }
}
