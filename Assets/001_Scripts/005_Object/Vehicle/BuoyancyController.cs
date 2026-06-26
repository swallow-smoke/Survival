using _001_Scripts.Controller;
using UnityEngine;

namespace _001_Scripts.Structure
{
    [RequireComponent(typeof(Rigidbody))]
    public class BuoyancyController : MonoBehaviour
    {
        [SerializeField] private Rigidbody _rb;

        [Header("부력 설정")]
        [Tooltip("오브젝트 부피 (m³). 0이면 Renderer bounds에서 자동 계산")]
        [SerializeField] private float volume = 0f;
        [Tooltip("물 밀도 (kg/m³). 기본 1000")]
        [SerializeField] private float waterDensity = 1000f;
        [Tooltip("수중 Linear Drag")]
        [SerializeField] private float waterLinearDrag = 3f;
        [Tooltip("수중 Angular Drag")]
        [SerializeField] private float waterAngularDrag = 3f;

        [Header("비상 부력")]
        [Tooltip("비상 부력 활성화 시 추가 상승 가속도")]
        [SerializeField] private float emergencyBuoyancyAccel = 9.81f;

        [Header("부력 콜라이더")]
        [Tooltip("부력 판정용 BoxCollider 크기. (0,0,0)이면 자동 계산")]
        [SerializeField] private Vector3 buoyancyBoxSize = Vector3.zero;
        [SerializeField] private Vector3 buoyancyBoxOffset = Vector3.zero;

        private BoxCollider _buoyancyCollider;
        private WaterVolume _currentWater;
        private float _effectiveVolume;
        private float _airLinearDrag;
        private float _airAngularDrag;
        private bool _emergencyActive;

        public bool IsInWater => _currentWater != null;
        public float SubmergedRatio { get; private set; }
        public bool IsEmergencyActive => _emergencyActive;
        public bool OverrideVertical { get; set; }

        public System.Action OnEnterWater;
        public System.Action OnExitWater;

        private void Awake()
        {
            _airLinearDrag = _rb.linearDamping;
            _airAngularDrag = _rb.angularDamping;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;

            _buoyancyCollider = BuoyancyColliderBuilder.Build(this, buoyancyBoxSize, buoyancyBoxOffset);
            CalculateVolume();
        }

        private void CalculateVolume()
        {
            if (volume > 0f)
            {
                _effectiveVolume = volume;
                return;
            }

            var size = _buoyancyCollider.size;
            _effectiveVolume = size.x * size.y * size.z
                               * transform.lossyScale.x
                               * transform.lossyScale.y
                               * transform.lossyScale.z;
        }

        private void FixedUpdate()
        {
            if (_currentWater == null)
            {
                SubmergedRatio = 0f;
                return;
            }

            SubmergedRatio = CalculateSubmergedRatio();
            if (!OverrideVertical) ApplyBuoyancy();
            ApplyDrag();

            if (_emergencyActive)
                _rb.AddForce(Vector3.up * emergencyBuoyancyAccel * _rb.mass, ForceMode.Force);
        }

        private float CalculateSubmergedRatio()
        {
            float surfaceY = _currentWater.GetSurfaceY();
            Bounds bounds = _buoyancyCollider.bounds;
            return Mathf.Clamp01((surfaceY - bounds.min.y) / bounds.size.y);
        }

        private void ApplyBuoyancy()
        {
            float submergedVolume = _effectiveVolume * SubmergedRatio;
            float buoyancyForce = waterDensity * Mathf.Abs(Physics.gravity.y) * submergedVolume;
            _rb.AddForce(Vector3.up * buoyancyForce, ForceMode.Force);
        }

        private void ApplyDrag()
        {
            _rb.linearDamping = Mathf.Lerp(_airLinearDrag, waterLinearDrag, SubmergedRatio);
            _rb.angularDamping = Mathf.Lerp(_airAngularDrag, waterAngularDrag, SubmergedRatio);
        }

        public void HandleEnterWater(WaterVolume water)
        {
            _currentWater = water;
            _rb.useGravity = false;
            OnEnterWater?.Invoke();
        }

        public void HandleExitWater(WaterVolume water)
        {
            if (_currentWater != water) return;

            _currentWater = null;
            _rb.useGravity = true;
            _rb.linearDamping = _airLinearDrag;
            _rb.angularDamping = _airAngularDrag;
            SubmergedRatio = 0f;
            OnExitWater?.Invoke();
        }

        public void ActivateEmergencyBuoyancy() => _emergencyActive = true;
        public void DeactivateEmergencyBuoyancy() => _emergencyActive = false;

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (_buoyancyCollider != null)
            {
                Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f);
                Gizmos.matrix = _buoyancyCollider.transform.localToWorldMatrix;
                Gizmos.DrawCube(_buoyancyCollider.center, _buoyancyCollider.size);

                Gizmos.color = new Color(0f, 0.5f, 1f, 0.8f);
                Gizmos.DrawWireCube(_buoyancyCollider.center, _buoyancyCollider.size);
                return;
            }

            Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f);
            Gizmos.matrix = Matrix4x4.TRS(
                transform.TransformPoint(buoyancyBoxOffset),
                transform.rotation,
                transform.lossyScale
            );
            Vector3 previewSize = buoyancyBoxSize != Vector3.zero ? buoyancyBoxSize : Vector3.one;
            Gizmos.DrawCube(Vector3.zero, previewSize);

            Gizmos.color = new Color(0f, 0.5f, 1f, 0.8f);
            Gizmos.DrawWireCube(Vector3.zero, previewSize);
        }
#endif
    }
}