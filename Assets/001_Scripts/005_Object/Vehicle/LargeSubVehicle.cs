using _001_Scripts.Data.Structure.Interface;
using _001_Scripts.Interface;
using _001_Scripts.Object.Vehicle;
using _001_Scripts.Vehicle.Core;
using UnityEngine;

namespace _001_Scripts.Structure
{
    [RequireComponent(typeof(Rigidbody))]
    public class LargeSubVehicle : VehicleBody, IVehicleControllable, ISurfaceDetectable
    {
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float verticalSpeed = 3f;
        [SerializeField] private float accelForce = 30f;
        [SerializeField] private Rigidbody _rb;
        [SerializeField] private Transform _cameraAnchor;
        [SerializeField] private Transform _interiorAnchor;
        [SerializeField] private BuoyancyController _buoyancy;

        [Header("Emergency Buoyancy Module")]
        [Tooltip("외장 모듈 시스템 연동 전까지 임시 플래그")]
        [SerializeField] private bool hasEmergencyBuoyancyModule;

        private Vector2 moveInput;
        private float verticalInput;
        private bool isControlled;

        public Transform CameraAnchor => _cameraAnchor;
        public Transform InteriorAnchor => _interiorAnchor;

        public void EnterControl()
        {
            moveInput = Vector2.zero;
            verticalInput = 0f;
            isControlled = true;
        }

        public void ExitControl() => isControlled = false;

        public void HandleLook(Vector2 mouseDelta) { }

        public void HandleMove(Vector2 wasd) => moveInput = wasd;
        public void HandleVertical(float value) => verticalInput = value;

        private void FixedUpdate()
        {
            if (fuel <= 0f && hasEmergencyBuoyancyModule)
            {
                _buoyancy.ActivateEmergencyBuoyancy();
                return;
            }

            if (!isControlled) return;

            Vector3 dir = transform.forward * moveInput.y + transform.right * moveInput.x;
            Vector3 horizontalTarget = dir.normalized * moveSpeed;
            Vector3 targetVelocity = new Vector3(horizontalTarget.x, verticalInput * verticalSpeed, horizontalTarget.z);

            Vector3 velocityDiff = targetVelocity - _rb.linearVelocity;
            _rb.AddForce(velocityDiff * accelForce, ForceMode.Force);
        }

        private void OnDestroy()
        {
            if (_interiorAnchor == null) return;
            for (int i = _interiorAnchor.childCount - 1; i >= 0; i--)
                _interiorAnchor.GetChild(i).SetParent(null);
        }

        public void OnReachedSurface()
        {
            _buoyancy.DeactivateEmergencyBuoyancy();
        }
    }
}
