using System;
using AstraNope.Core.World.Water.Interfaces;
using UnityEngine;
using UnityEngine.Serialization;

namespace AstraNope.Core.World.Water
{
    [Serializable]
    public struct WaterWave
    {
        public Vector2 direction;
        [Min(0f)] public float amplitude;
        [Min(0.01f)] public float wavelength;
        public float speed;
    }

    [AddComponentMenu("Survival/Water/Ocean Water Body")]
    public class OceanBody : WaterBodyBehaviour
    {
        private const float InfinitePreviewSize = 1000000f;

        [FormerlySerializedAs("seaLevel")]
        [SerializeField] private float seaLevel;
        [SerializeField] private bool infinite = true;
        [SerializeField] private Vector2 finiteSize = new Vector2(500f, 500f);
        [SerializeField] private bool useWaves;
        [SerializeField] private WaterWave[] waves = Array.Empty<WaterWave>();
        [SerializeField] private Vector2 currentDirection = Vector2.right;
        [Min(0f), SerializeField] private float currentSpeed;
        [SerializeField] private Renderer surfaceRenderer;
        [Tooltip("Water membership is query-based. Disable the legacy surface collider to keep it out of Physics.")]
        [SerializeField] private bool disableSurfaceCollider = true;

        private MaterialPropertyBlock _propertyBlock;

        public bool IsInfinite => infinite;
        public float SeaLevel => seaLevel;

        protected override void OnEnable()
        {
            base.OnEnable();
            SyncRendererProperties();
            ApplySurfaceColliderPolicy();
        }

        public override Bounds WorldBounds
        {
            get
            {
                Vector2 size = infinite ? Vector2.one * InfinitePreviewSize : finiteSize;
                return new Bounds(new Vector3(transform.position.x, seaLevel, transform.position.z),
                    new Vector3(Mathf.Max(0.01f, size.x), InfinitePreviewSize, Mathf.Max(0.01f, size.y)));
            }
        }

        public override bool TrySample(Vector3 worldPosition, out WaterSample sample)
        {
            if (!infinite)
            {
                Vector3 local = transform.InverseTransformPoint(worldPosition);
                if (Mathf.Abs(local.x) > finiteSize.x * 0.5f || Mathf.Abs(local.z) > finiteSize.y * 0.5f)
                {
                    sample = default;
                    return false;
                }
            }

            float time = Application.isPlaying ? Time.time : 0f;
            SampleSurface(worldPosition, time, out float height, out Vector3 normal);
            Vector2 current = currentDirection.sqrMagnitude > 0.000001f
                ? currentDirection.normalized * currentSpeed
                : Vector2.zero;
            Vector3 surface = new Vector3(worldPosition.x, height, worldPosition.z);
            sample = new WaterSample(this, worldPosition, surface, normal,
                new Vector3(current.x, 0f, current.y), WaterBodyType.Ocean);
            return true;
        }

        private void SampleSurface(Vector3 position, float time, out float height, out Vector3 normal)
        {
            height = seaLevel;
            float derivativeX = 0f;
            float derivativeZ = 0f;

            if (useWaves && waves != null)
            {
                int count = Mathf.Min(4, waves.Length);
                for (int i = 0; i < count; i++)
                {
                    WaterWave wave = waves[i];
                    if (wave.amplitude <= 0f || wave.wavelength <= 0.001f) continue;
                    Vector2 direction = wave.direction.sqrMagnitude > 0.000001f
                        ? wave.direction.normalized
                        : Vector2.right;
                    float frequency = 2f * Mathf.PI / wave.wavelength;
                    float phase = frequency * (direction.x * position.x + direction.y * position.z) + wave.speed * time;
                    height += wave.amplitude * Mathf.Sin(phase);
                    float slope = wave.amplitude * frequency * Mathf.Cos(phase);
                    derivativeX += slope * direction.x;
                    derivativeZ += slope * direction.y;
                }
            }

            normal = new Vector3(-derivativeX, 1f, -derivativeZ).normalized;
        }

        private void SyncRendererProperties()
        {
            if (surfaceRenderer == null) surfaceRenderer = GetComponent<Renderer>();
            if (surfaceRenderer == null) return;
            if (_propertyBlock == null) _propertyBlock = new MaterialPropertyBlock();
            surfaceRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetFloat("_UseWaves", useWaves ? 1f : 0f);
            Vector4 speeds = Vector4.zero;
            for (int i = 0; i < 4; i++)
            {
                WaterWave wave = waves != null && i < waves.Length ? waves[i] : default;
                Vector2 direction = wave.direction.sqrMagnitude > 0.000001f ? wave.direction.normalized : Vector2.right;
                _propertyBlock.SetVector("_Wave" + i,
                    new Vector4(direction.x, direction.y, wave.amplitude, Mathf.Max(0.01f, wave.wavelength)));
                speeds[i] = wave.speed;
            }
            _propertyBlock.SetVector("_WaveSpeed", speeds);
            _propertyBlock.SetFloat("_FlowSpeed", currentSpeed);
            surfaceRenderer.SetPropertyBlock(_propertyBlock);
        }

        private void ApplySurfaceColliderPolicy()
        {
            if (!disableSurfaceCollider) return;
            if (surfaceRenderer == null) surfaceRenderer = GetComponent<Renderer>();

            GameObject surfaceObject = surfaceRenderer != null ? surfaceRenderer.gameObject : gameObject;
            Collider surfaceCollider = surfaceObject.GetComponent<Collider>();
            if (surfaceCollider != null) surfaceCollider.enabled = false;
        }

        [Obsolete("Use TrySample.")]
        public float GetSurfaceY(Vector3 position) =>
            TrySample(position, out WaterSample sample) ? sample.SurfaceHeight : seaLevel;

        [Obsolete("Use TrySample and inspect SignedDepth.")]
        public bool Contain(Vector3 position) =>
            TrySample(position, out WaterSample sample) && sample.IsSubmerged;

#if UNITY_EDITOR
        private void OnValidate()
        {
            SyncRendererProperties();
            ApplySurfaceColliderPolicy();
            NotifyBoundsChanged();
        }

        private void OnDrawGizmosSelected()
        {
            if (!ShowGizmos) return;
            Gizmos.color = new Color(0f, 0.55f, 1f, 0.55f);
            Vector2 size = infinite ? Vector2.one * 100f : finiteSize;
            Gizmos.DrawWireCube(new Vector3(transform.position.x, seaLevel, transform.position.z),
                new Vector3(size.x, 0.05f, size.y));
        }
#endif
    }
}
