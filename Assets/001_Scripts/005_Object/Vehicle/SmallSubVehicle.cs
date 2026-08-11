using _001_Scripts.Core._000_World._001_Water.Interface;
using _001_Scripts.Entities;
using _001_Scripts.Object.Vehicle;
using _001_Scripts.Vehicle.Core;
using UnityEngine;
using VContainer;
using EntityVehicle = _001_Scripts.Entities.Vehicle;

namespace _001_Scripts.Structure
{
    [RequireComponent(typeof(Rigidbody), typeof(Entity), typeof(Health))]
    [RequireComponent(typeof(EntityVehicle), typeof(Submarine))]
    public class SmallSubVehicle : MonoBehaviour, IVehicleControllable
    {
        [SerializeField] private float moveSpeed = 10f;
        [SerializeField] private float turnSensitivity = 2f;
        [SerializeField] private float pitchMin = -80f;
        [SerializeField] private float pitchMax = 80f;
        [SerializeField] private float accelForce = 50f;
        [SerializeField] private Rigidbody _rb;
        [SerializeField] private Transform _cameraAnchor;

        [Header("수중 저항")]
        [SerializeField] private float waterLinearDrag = 3f;
        [SerializeField] private float waterAngularDrag = 3f;
        [SerializeField] private float gravityNeutralizeSpeed = 2f;

        [Header("회전 댐핑")]
        [SerializeField] private float rotationDamping = 5f;

        private Quaternion _targetRotation;
        private float _airLinearDrag;
        private float _airAngularDrag;
        private float pitch, yaw;
        private Vector2 moveInput;
        private float verticalInput;
        private bool isControlled;
        private IWaterQueryService _waterQuery;
        private BuoyancyController _buoyancy;

        public Transform CameraAnchor => _cameraAnchor;

        private void Awake()
        {
            if (!GetComponent<Entity>()) gameObject.AddComponent<Entity>();
            if (!GetComponent<Submarine>()) gameObject.AddComponent<Submarine>();
            _airLinearDrag = _rb.linearDamping;
            _airAngularDrag = _rb.angularDamping;
            _buoyancy = GetComponent<BuoyancyController>();
            if (_buoyancy == null) _buoyancy = gameObject.AddComponent<BuoyancyController>();
            _buoyancy.SetMode(BuoyancyMode.NeutralBuoyancy);
        }

        public void EnterControl()
        {
            pitch = transform.eulerAngles.x;
            yaw = transform.eulerAngles.y;
            _targetRotation = transform.rotation;
            moveInput = Vector2.zero;
            isControlled = true;
        }

        public void ExitControl() => isControlled = false;

        public void HandleLook(Vector2 mouseDelta)
        {
            yaw += mouseDelta.x * turnSensitivity;
            pitch -= mouseDelta.y * turnSensitivity;
            pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
            _targetRotation = Quaternion.Euler(pitch, yaw, 0);
        }

        public void HandleMove(Vector2 wasd) => moveInput = wasd;
        public void HandleVertical(float value) => verticalInput = value;

        private void FixedUpdate()
        {
            if (!isControlled) return;

            transform.rotation = Quaternion.Slerp(transform.rotation, _targetRotation, rotationDamping * Time.fixedDeltaTime);

            Vector3 forwardMove = transform.forward * moveInput.y;
            Vector3 strafe = transform.right * moveInput.x;
            Vector3 dir = (forwardMove + strafe).normalized;

            Vector3 targetVelocity = dir * moveSpeed + transform.up * (verticalInput * moveSpeed);
            Vector3 velocityDiff = targetVelocity - _rb.linearVelocity;
            _rb.AddForce(velocityDiff * accelForce, ForceMode.Force);
        }

        [Inject]
        public void Construct(IWaterQueryService waterQuery)
        {
            _waterQuery = waterQuery;
            if (_buoyancy != null) _buoyancy.Configure(waterQuery);
        }
    }
}
