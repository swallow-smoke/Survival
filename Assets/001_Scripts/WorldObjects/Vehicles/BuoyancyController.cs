using System;
using AstraNope.Core.World.Water;
using AstraNope.Core.World.Water.Interfaces;
using UnityEngine;
using VContainer;

namespace AstraNope.WorldObjects.Vehicles
{
    public enum BuoyancyMode
    {
        NaturalBuoyancy,
        NeutralBuoyancy,
        ControlledDepth,
        EmergencyAscent
    }

    [Serializable]
    public struct BuoyancyPoint
    {
        public Transform Transform;
        [Min(0f)] public float Weight;
        [Min(0.01f)] public float Radius;
    }

    [RequireComponent(typeof(Rigidbody))]
    [AddComponentMenu("Survival/Water/Buoyancy Controller")]
    public class BuoyancyController : MonoBehaviour
    {
        [SerializeField] private Rigidbody _rb;
        [SerializeField] private WaterPhysicsProfile physicsProfile;
        [SerializeField] private BuoyancyMode mode = BuoyancyMode.NaturalBuoyancy;

        [Header("Hull")]
        [Tooltip("Object volume in cubic metres. Zero estimates from the buoyancy box.")]
        [Min(0f), SerializeField] private float volume;
        [SerializeField] private Vector3 buoyancyBoxSize = Vector3.zero;
        [SerializeField] private Vector3 buoyancyBoxOffset = Vector3.zero;
        [SerializeField] private BuoyancyPoint[] buoyancyPoints = Array.Empty<BuoyancyPoint>();
        [Tooltip("Sleeping rigidbodies do not need repeated water queries until Physics wakes them.")]
        [SerializeField] private bool skipSleepingBodies = true;
        [SerializeField, Tooltip("물 영역 밖에서는 Rigidbody 중력을 항상 복구합니다.")]
        private bool forceGravityOutsideWater = true;

        [Header("Fallback Physics")]
        [Min(0f), SerializeField] private float waterDensity = 1000f;
        [Min(0f), SerializeField] private float waterLinearDrag = 3f;
        [Min(0f), SerializeField] private float waterAngularDrag = 3f;
        [Min(0f), SerializeField] private float flowDrag = 1.5f;
        [Min(0f), SerializeField] private float buoyancyMultiplier = 1f;
        [Min(0f), SerializeField] private float maximumForce = 100000f;
        [Min(0f), SerializeField] private float verticalDamping = 2f;
        [Range(0f, 1f), SerializeField] private float surfaceNormalInfluence = 0.15f;

        [Header("Controlled / Emergency")]
        [SerializeField] private float targetSurfaceDepth = 2f;
        [Min(0f), SerializeField] private float depthHoldStrength = 4f;
        [Min(0f), SerializeField] private float emergencyBuoyancyAccel = 9.81f;

        private IWaterQueryService _waterQuery;
        private float _effectiveVolume;
        private float _airLinearDamping;
        private float _airAngularDamping;
        private bool _originalUseGravity;
        private bool _emergencyActive;
        private bool _wasInWater;
        private IWaterBody _currentWater;

        public bool IsInWater => _currentWater != null;
        public float SubmergedRatio { get; private set; }
        public bool IsEmergencyActive => _emergencyActive;
        public bool OverrideVertical { get; set; }
        public BuoyancyMode Mode => mode;
        public Action OnEnterWater;
        public Action OnExitWater;

        private void Awake()
        {
            if (_rb == null) _rb = GetComponent<Rigidbody>();
            _airLinearDamping = _rb.linearDamping;
            _airAngularDamping = _rb.angularDamping;
            _originalUseGravity = _rb.useGravity;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            CalculateVolume();
        }

        private void OnEnable()
        {
            if (_waterQuery == null) _waterQuery = WaterRegistryLocator.Current as IWaterQueryService;
        }

        private void FixedUpdate()
        {
            if (_waterQuery == null || _rb == null)
            {
                RestoreDrySettings();
                return;
            }

            if (skipSleepingBodies && _rb.IsSleeping()) return;

            int pointCount = buoyancyPoints != null && buoyancyPoints.Length > 0 ? buoyancyPoints.Length : 5;
            float totalWeight = GetTotalWeight(pointCount);
            float submergedWeight = 0f;
            IWaterBody selectedBody = null;

            for (int i = 0; i < pointCount; i++)
            {
                GetPoint(i, pointCount, out Vector3 point, out float pointWeight, out float radius);
                float normalizedWeight = pointWeight / totalWeight;
                if (!_waterQuery.TrySample(point, out WaterSample sample) || sample.SignedDepth <= 0f) continue;

                float submersion = Mathf.Clamp01(sample.SubmersionDepth / Mathf.Max(0.01f, radius * 2f));
                submergedWeight += normalizedWeight * submersion;
                if (selectedBody == null || sample.WaterBody.Priority > selectedBody.Priority)
                    selectedBody = sample.WaterBody;

                ApplyForces(point, radius, normalizedWeight, submersion, sample);
            }

            SubmergedRatio = Mathf.Clamp01(submergedWeight);
            _currentWater = selectedBody;
            UpdateWaterTransition(selectedBody != null);

            if (selectedBody == null)
            {
                RestoreDrySettings();
                return;
            }

            _rb.linearDamping = Mathf.Lerp(_airLinearDamping, ProfileLinearDamping, SubmergedRatio);
            _rb.angularDamping = Mathf.Lerp(_airAngularDamping, ProfileAngularDamping, SubmergedRatio);

            if (_emergencyActive || mode == BuoyancyMode.EmergencyAscent)
                _rb.AddForce(Vector3.up * Mathf.Min(emergencyBuoyancyAccel * _rb.mass, ProfileMaximumForce), ForceMode.Force);
        }

        private void ApplyForces(Vector3 point, float radius, float pointWeight,
            float submersion, WaterSample sample)
        {
            Vector3 pointVelocity = _rb.GetPointVelocity(point);
            Vector3 liftDirection = Vector3.Slerp(Vector3.up, sample.SurfaceNormal, surfaceNormalInfluence).normalized;
            float gravity = Physics.gravity.magnitude;
            float lift;

            if (mode == BuoyancyMode.NeutralBuoyancy)
            {
                lift = _rb.mass * gravity * pointWeight * submersion;
            }
            else
            {
                float displacedVolume = _effectiveVolume * pointWeight * submersion;
                lift = ProfileDensity * gravity * displacedVolume * ProfileBuoyancyMultiplier;
            }

            if (OverrideVertical) lift = 0f;
            lift = Mathf.Min(lift, ProfileMaximumForce * pointWeight);
            _rb.AddForceAtPosition(liftDirection * lift, point, ForceMode.Force);

            float verticalSpeed = Vector3.Dot(pointVelocity, sample.SurfaceNormal);
            Vector3 dampingForce = -sample.SurfaceNormal * verticalSpeed * verticalDamping * _rb.mass * pointWeight * submersion;
            dampingForce = Vector3.ClampMagnitude(dampingForce, ProfileMaximumForce * pointWeight);
            _rb.AddForceAtPosition(dampingForce, point, ForceMode.Force);

            Vector3 relativeVelocity = sample.FlowVelocity - pointVelocity;
            Vector3 flowForce = relativeVelocity * ProfileFlowDrag * _rb.mass * pointWeight * submersion;
            flowForce = Vector3.ClampMagnitude(flowForce, ProfileMaximumForce * pointWeight);
            _rb.AddForceAtPosition(flowForce, point, ForceMode.Force);

            if (mode == BuoyancyMode.ControlledDepth && !OverrideVertical)
            {
                float error = sample.SignedDepth - targetSurfaceDepth;
                float hold = Mathf.Clamp(error * depthHoldStrength * _rb.mass * pointWeight,
                    -ProfileMaximumForce * pointWeight, ProfileMaximumForce * pointWeight);
                _rb.AddForceAtPosition(Vector3.up * hold, point, ForceMode.Force);
            }
        }

        private void CalculateVolume()
        {
            if (volume > 0f)
            {
                _effectiveVolume = volume;
                return;
            }

            Vector3 size = buoyancyBoxSize;
            if (size == Vector3.zero)
            {
                Renderer[] renderers = GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    Bounds bounds = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
                    size = new Vector3(bounds.size.x / Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.x)),
                        bounds.size.y / Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.y)),
                        bounds.size.z / Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.z)));
                    buoyancyBoxOffset = transform.InverseTransformPoint(bounds.center);
                }
                else
                {
                    size = Vector3.one;
                }
            }

            buoyancyBoxSize = size;
            Vector3 scale = transform.lossyScale;
            _effectiveVolume = Mathf.Max(0.001f,
                Mathf.Abs(size.x * size.y * size.z * scale.x * scale.y * scale.z));
        }

        private float GetTotalWeight(int pointCount)
        {
            if (buoyancyPoints == null || buoyancyPoints.Length == 0) return pointCount;
            float total = 0f;
            for (int i = 0; i < buoyancyPoints.Length; i++) total += Mathf.Max(0.0001f, buoyancyPoints[i].Weight);
            return Mathf.Max(0.0001f, total);
        }

        private void GetPoint(int index, int pointCount, out Vector3 position, out float weight, out float radius)
        {
            if (buoyancyPoints != null && buoyancyPoints.Length > 0)
            {
                BuoyancyPoint point = buoyancyPoints[index];
                position = point.Transform != null ? point.Transform.position : transform.position;
                weight = Mathf.Max(0.0001f, point.Weight);
                radius = Mathf.Max(0.01f, point.Radius);
                return;
            }

            Vector3 half = buoyancyBoxSize * 0.5f;
            Vector3 local;
            switch (index)
            {
                case 0: local = new Vector3(-half.x, -half.y * 0.5f, -half.z); break;
                case 1: local = new Vector3(half.x, -half.y * 0.5f, -half.z); break;
                case 2: local = new Vector3(-half.x, -half.y * 0.5f, half.z); break;
                case 3: local = new Vector3(half.x, -half.y * 0.5f, half.z); break;
                default: local = Vector3.zero; break;
            }

            position = transform.TransformPoint(buoyancyBoxOffset + local);
            weight = 1f;
            radius = Mathf.Max(0.05f, buoyancyBoxSize.y * Mathf.Abs(transform.lossyScale.y) * 0.25f);
        }

        private void UpdateWaterTransition(bool inWater)
        {
            if (inWater == _wasInWater) return;
            _wasInWater = inWater;
            if (inWater) OnEnterWater?.Invoke();
            else OnExitWater?.Invoke();
        }

        private void RestoreDrySettings()
        {
            _currentWater = null;
            SubmergedRatio = 0f;
            if (_rb == null) return;
            _rb.linearDamping = _airLinearDamping;
            _rb.angularDamping = _airAngularDamping;
            _rb.useGravity = forceGravityOutsideWater || _originalUseGravity;
            UpdateWaterTransition(false);
        }

        private void OnDisable() => RestoreDrySettings();
        private void OnDestroy() => RestoreDrySettings();

        [Inject]
        public void Construct(IWaterQueryService waterQuery) => _waterQuery = waterQuery;
        public void Configure(IWaterQueryService waterQuery) => _waterQuery = waterQuery;
        public void SetMode(BuoyancyMode buoyancyMode) => mode = buoyancyMode;
        public void SetTargetSurfaceDepth(float depthValue) => targetSurfaceDepth = Mathf.Max(0f, depthValue);

        public void ActivateEmergencyBuoyancy() => _emergencyActive = true;
        public void DeactivateEmergencyBuoyancy() => _emergencyActive = false;

        private float ProfileDensity => physicsProfile != null ? physicsProfile.Density : waterDensity;
        private float ProfileLinearDamping => physicsProfile != null ? physicsProfile.LinearDamping : waterLinearDrag;
        private float ProfileAngularDamping => physicsProfile != null ? physicsProfile.AngularDamping : waterAngularDrag;
        private float ProfileFlowDrag => physicsProfile != null ? physicsProfile.FlowDrag : flowDrag;
        private float ProfileBuoyancyMultiplier => physicsProfile != null ? physicsProfile.BuoyancyMultiplier : buoyancyMultiplier;
        private float ProfileMaximumForce => physicsProfile != null ? physicsProfile.MaximumForce : maximumForce;

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            int pointCount = buoyancyPoints != null && buoyancyPoints.Length > 0 ? buoyancyPoints.Length : 5;
            for (int i = 0; i < pointCount; i++)
            {
                GetPoint(i, pointCount, out Vector3 point, out _, out float radius);
                Gizmos.color = IsInWater ? Color.cyan : new Color(0f, 0.55f, 1f, 0.75f);
                Gizmos.DrawWireSphere(point, radius);
            }
        }
#endif
    }
}
