using _001_Scripts.Core._000_World._001_Water.Interface;
using UnityEngine;
using UnityEngine.Serialization;

namespace _001_Scripts.Core._000_World._001_Water
{
    [RequireComponent(typeof(BoxCollider))]
    [AddComponentMenu("Survival/Water/Lake Water Body")]
    public class LakeWaterBody : WaterBodyBehaviour, IWaterbody
    {
        [SerializeField] private BoxCollider volume;
        [FormerlySerializedAs("surfaceTransform")]
        [SerializeField] private Transform surface;
        [Min(0.01f), SerializeField] private float depth = 5f;
        [Min(0f), SerializeField] private float sampleHeightAboveSurface = 2f;
        [SerializeField] private Vector3 flowDirection;
        [Min(0f), SerializeField] private float flowSpeed;

        protected virtual void Awake()
        {
            if (volume == null) volume = GetComponent<BoxCollider>();
            ValidateCollider();
        }

        public override Bounds WorldBounds
        {
            get
            {
                if (volume == null) return new Bounds(transform.position, Vector3.zero);
                Bounds bounds = volume.bounds;
                float surfaceHeight = SurfaceHeight;
                float minimum = surfaceHeight - depth;
                float maximum = surfaceHeight + sampleHeightAboveSurface;
                bounds.Encapsulate(new Vector3(bounds.center.x, minimum, bounds.center.z));
                bounds.Encapsulate(new Vector3(bounds.center.x, maximum, bounds.center.z));
                return bounds;
            }
        }

        public float SurfaceHeight => surface != null ? surface.position.y : transform.position.y;

        public override bool TrySample(Vector3 worldPosition, out WaterSample sample)
        {
            if (volume == null)
            {
                sample = default;
                return false;
            }

            Vector3 local = volume.transform.InverseTransformPoint(worldPosition) - volume.center;
            Vector3 half = volume.size * 0.5f;
            if (Mathf.Abs(local.x) > half.x || Mathf.Abs(local.z) > half.z)
            {
                sample = default;
                return false;
            }

            float surfaceHeight = SurfaceHeight;
            if (worldPosition.y < surfaceHeight - depth || worldPosition.y > surfaceHeight + sampleHeightAboveSurface)
            {
                sample = default;
                return false;
            }

            Vector3 velocity = flowDirection.sqrMagnitude > 0.000001f
                ? flowDirection.normalized * flowSpeed
                : Vector3.zero;
            sample = new WaterSample(this, worldPosition,
                new Vector3(worldPosition.x, surfaceHeight, worldPosition.z), Vector3.up,
                velocity, WaterBodyType.Lake);
            return true;
        }

        private void ValidateCollider()
        {
            if (volume != null && !volume.isTrigger)
                Debug.LogWarning($"[{nameof(LakeWaterBody)}] {name}: query BoxCollider should be a Trigger.", this);
        }

        [System.Obsolete("Use TrySample.")]
        public float GetSurfaceY(Vector3 position) => SurfaceHeight;

        [System.Obsolete("Use TrySample and inspect SignedDepth.")]
        public bool Contain(Vector3 position) =>
            TrySample(position, out WaterSample sample) && sample.IsSubmerged;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (volume == null) volume = GetComponent<BoxCollider>();
            depth = Mathf.Max(0.01f, depth);
            NotifyBoundsChanged();
        }

        private void OnDrawGizmosSelected()
        {
            if (!ShowGizmos || volume == null) return;
            Gizmos.color = new Color(0f, 0.7f, 1f, 0.25f);
            Gizmos.matrix = volume.transform.localToWorldMatrix;
            Gizmos.DrawCube(volume.center, volume.size);
            Gizmos.color = new Color(0f, 0.7f, 1f, 0.8f);
            Gizmos.DrawWireCube(volume.center, volume.size);
        }
#endif
    }
}
