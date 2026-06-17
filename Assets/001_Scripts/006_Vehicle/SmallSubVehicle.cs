using _001_Scripts.Data.Structure.Interface;
using _001_Scripts.Vehicle.Core;
using UnityEngine;

namespace _001_Scripts.Structure
{
    [RequireComponent(typeof(Rigidbody))]
    public class SmallSubVehicle : VehicleBody, IVehicleControllable
    {
        [SerializeField] private float moveSpeed = 10f;
        [SerializeField] private float turnSensitivity = 2f;
        [SerializeField] private float pitchMin = -80f;
        [SerializeField] private float pitchMax = 80f;
        [SerializeField] private float accelForce = 50f;
        [SerializeField] private Rigidbody _rb;
        [SerializeField] private Transform _cameraAnchor;
        [SerializeField] private LayerMask waterLayer;

        [Header("수중 저항")]
        [SerializeField] private float waterLinearDrag = 3f;
        [SerializeField] private float waterAngularDrag = 3f;
        [SerializeField] private float gravityNeutralizeSpeed = 2f;
        
        [Header("회전 댐핑")]
        [SerializeField] private float rotationDamping = 5f;

        private Quaternion _targetRotation;

        private float _airLinearDrag;
        private float _airAngularDrag;
        private float _gravityScale = 1f;
        private float pitch, yaw;
        private Vector2 moveInput;
        private bool isControlled;
        private bool isInWater;

        public Transform CameraAnchor => _cameraAnchor;

        private void Awake()
        {
            _airLinearDrag = _rb.linearDamping;
            _airAngularDrag = _rb.angularDamping;
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
            // legacy
            // transform.rotation = Quaternion.Euler(pitch, yaw, 0);
            _targetRotation = Quaternion.Euler(pitch, yaw, 0);
        }

        public void HandleMove(Vector2 wasd) => moveInput = wasd;
        public void HandleVertical(float value) { }

        private void OnTriggerEnter(Collider other)
        {
            if ((waterLayer.value & (1 << other.gameObject.layer)) != 0)
            {
                isInWater = true;
                _rb.linearDamping = waterLinearDrag;
                _rb.angularDamping = waterAngularDrag;
            }
        }

        private void FixedUpdate()
        {
            _gravityScale = isInWater
                ? Mathf.MoveTowards(_gravityScale, 0f, gravityNeutralizeSpeed * Time.fixedDeltaTime)
                : Mathf.MoveTowards(_gravityScale, 1f, gravityNeutralizeSpeed * Time.fixedDeltaTime);

            _rb.useGravity = false;
            _rb.AddForce(Physics.gravity * _rb.mass * _gravityScale, ForceMode.Force);

            if (!isControlled) return;

            transform.rotation = Quaternion.Slerp(transform.rotation, _targetRotation, rotationDamping * Time.fixedDeltaTime);

            Vector3 forwardMove = transform.forward * moveInput.y;
            Vector3 strafe = transform.right * moveInput.x;
            Vector3 dir = (forwardMove + strafe).normalized;

            Vector3 targetVelocity = dir * moveSpeed;
            Vector3 velocityDiff = targetVelocity - _rb.linearVelocity;
            _rb.AddForce(velocityDiff * accelForce, ForceMode.Force);
        }
    }
}