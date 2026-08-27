using AstraNope.Contracts.WorldObjects;
using AstraNope.WorldObjects.Entities;
using AstraNope.Contracts;
using AstraNope.WorldObjects.Vehicles;
using AstraNope.WorldObjects.Vehicles.Core;
using UnityEngine;
using EntityVehicle = AstraNope.WorldObjects.Entities.Vehicle;

namespace AstraNope.WorldObjects.Vehicles
{
    [RequireComponent(typeof(Rigidbody), typeof(Entity), typeof(Health))]
    [RequireComponent(typeof(EntityVehicle), typeof(Submarine))]
    public class LargeSubVehicle : MonoBehaviour, IVehicleControllable, ISurfaceDetectable
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
        private EntityVehicle _vehicle;

        public Transform CameraAnchor => _cameraAnchor;
        public Transform InteriorAnchor => _interiorAnchor;

        private void Awake()
        {
            if (!GetComponent<Entity>()) gameObject.AddComponent<Entity>();
            if (!GetComponent<Submarine>()) gameObject.AddComponent<Submarine>();
            _vehicle = GetComponent<EntityVehicle>();
            if (_buoyancy == null) _buoyancy = GetComponent<BuoyancyController>();
            if (_buoyancy == null) _buoyancy = gameObject.AddComponent<BuoyancyController>();
        }

        public void EnterControl()
        {
            moveInput = Vector2.zero;
            verticalInput = 0f;
            isControlled = true;
        }

        public void ExitControl()
        {
            isControlled = false;
            if (_buoyancy != null) _buoyancy.OverrideVertical = false;
        }

        public void HandleLook(Vector2 mouseDelta) { }

        public void HandleMove(Vector2 wasd) => moveInput = wasd;
        public void HandleVertical(float value) => verticalInput = value;

        private void FixedUpdate()
        {
            if (_vehicle.Fuel <= 0f && hasEmergencyBuoyancyModule)
            {
                _buoyancy.ActivateEmergencyBuoyancy();
                return;
            }

            if (!isControlled) return;

            if (_buoyancy != null) _buoyancy.OverrideVertical = Mathf.Abs(verticalInput) > 0.01f;

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
