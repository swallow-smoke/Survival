using AstraNope.Core.World.Water;
using AstraNope.Core.World.Water.Interfaces;
using AstraNope.WorldObjects.Entities;
using AstraNope.WorldObjects.Vehicles;
using AstraNope.WorldObjects.Vehicles.Core;
using UnityEngine;
using VContainer;
using EntityVehicle = AstraNope.WorldObjects.Entities.Vehicle;
using AstraNope.Contracts;
using AstraNope.Contracts.WorldObjects;

namespace AstraNope.WorldObjects.Vehicles
{
    [RequireComponent(typeof(Rigidbody), typeof(Entity), typeof(Health))]
    [RequireComponent(typeof(EntityVehicle), typeof(Submarine))]
    public class SmallSubVehicle : MonoBehaviour, IVehicleControllable
    {
        public const int CurrentConfigurationVersion = 6;

        [SerializeField] private float moveSpeed = 10f;
        [SerializeField] private float turnSensitivity = 2f;
        [SerializeField] private float pitchMin = -80f;
        [SerializeField] private float pitchMax = 80f;
        [SerializeField] private float accelForce = 50f;
        [SerializeField, Min(.1f)] private float propulsionResponse = 3f;
        [SerializeField, Min(0f)] private float softSpeedLimit = 4f;
        [SerializeField] private Rigidbody _rb;
        [SerializeField] private Transform _cameraAnchor;
        [SerializeField, HideInInspector] private int configurationVersion;

        [Header("수중 저항")]
        [SerializeField, Tooltip("잠수함은 파도 표면의 부력, 법선, 유속 힘을 받지 않습니다.")]
        private bool ignoreSurfaceForces = true;
        [SerializeField] private float waterLinearDrag = .7f;
        [SerializeField] private float waterAngularDrag = 3f;
        [SerializeField, Min(.05f)] private float minimumSubmersionDepth = .8f;
        [SerializeField] private float airLinearDrag = .1f;
        [SerializeField] private float airAngularDrag = .5f;
        [SerializeField, Min(.1f)] private float waterEntryBlendDistance = 1.5f;
        [SerializeField, Min(.05f)] private float surfaceBrakeDistance = .8f;
        [SerializeField, Min(0f)] private float surfaceReturnStrength = 20f;
        [SerializeField, Min(0f)] private float surfaceReturnDamping = 8f;

        [Header("회전 댐핑")]
        [SerializeField] private float rotationDamping = 5f;
        [SerializeField, Min(.1f)] private float lookImpulseStrength = 8f;
        [SerializeField, Min(.1f)] private float angularCoastDamping = 4f;
        [SerializeField, Min(1f)] private float maximumTurnSpeed = 100f;

        private Quaternion _targetRotation;
        private float pitch, yaw;
        private Vector2 moveInput;
        private float verticalInput;
        private Vector2 _smoothedMoveInput;
        private float _smoothedVerticalInput;
        private Vector2 _lookAngularVelocity;
        private bool isControlled;
        private IWaterQueryService _waterQuery;
        private IInputService _input;
        private BuoyancyController _buoyancy;
        private bool _inputSubscribed;
        private float _ascendInput;
        private float _descendInput;
        private float _surfaceThrottle = 1f;

        public Transform CameraAnchor => _cameraAnchor;
        public int ConfigurationVersion => configurationVersion;

        public void Configure(Rigidbody body, Transform cameraAnchor, float speed = 10f,
            float acceleration = 8f, float lookSensitivity = .5f)
        {
            _rb = body;
            _cameraAnchor = cameraAnchor;
            moveSpeed = Mathf.Max(.1f, speed);
            accelForce = Mathf.Max(.1f, acceleration);
            turnSensitivity = Mathf.Max(.01f, lookSensitivity);
            ignoreSurfaceForces = true;
            waterLinearDrag = .7f;
            waterAngularDrag = 3f;
            propulsionResponse = 3f;
            softSpeedLimit = 4f;
            minimumSubmersionDepth = .8f;
            waterEntryBlendDistance = 1.5f;
            surfaceBrakeDistance = .8f;
            surfaceReturnStrength = 20f;
            surfaceReturnDamping = 8f;
            lookImpulseStrength = 8f;
            angularCoastDamping = 4f;
            maximumTurnSpeed = 100f;
            configurationVersion = CurrentConfigurationVersion;
        }

        private void Awake()
        {
            if (configurationVersion < CurrentConfigurationVersion)
            {
                moveSpeed = 10f;
                accelForce = 8f;
                turnSensitivity = .5f;
                ignoreSurfaceForces = true;
                waterLinearDrag = .7f;
                propulsionResponse = 3f;
                softSpeedLimit = 4f;
                waterEntryBlendDistance = 1.5f;
                surfaceBrakeDistance = .8f;
                surfaceReturnStrength = 20f;
                surfaceReturnDamping = 8f;
                lookImpulseStrength = 8f;
                angularCoastDamping = 4f;
                maximumTurnSpeed = 100f;
                configurationVersion = CurrentConfigurationVersion;
            }
            if (!_rb) _rb = GetComponent<Rigidbody>();
            if (!_cameraAnchor) _cameraAnchor = transform.Find("CameraAnchor");
            if (!GetComponent<Entity>()) gameObject.AddComponent<Entity>();
            if (!GetComponent<Submarine>()) gameObject.AddComponent<Submarine>();
            _buoyancy = GetComponent<BuoyancyController>();
            ConfigureUnderwaterPhysics();
        }

        public void EnterControl()
        {
            pitch = transform.eulerAngles.x;
            yaw = transform.eulerAngles.y;
            _targetRotation = transform.rotation;
            moveInput = Vector2.zero;
            _smoothedMoveInput = Vector2.zero;
            _smoothedVerticalInput = 0f;
            _lookAngularVelocity = Vector2.zero;
            _ascendInput = 0f;
            _descendInput = 0f;
            SubscribeInput();
            isControlled = true;
        }

        public void ExitControl()
        {
            isControlled = false;
            moveInput = Vector2.zero;
            verticalInput = 0f;
            _smoothedMoveInput = Vector2.zero;
            _smoothedVerticalInput = 0f;
            _lookAngularVelocity = Vector2.zero;
            _ascendInput = 0f;
            _descendInput = 0f;
            UnsubscribeInput();
        }

        public void HandleLook(Vector2 mouseDelta)
        {
            _lookAngularVelocity.x -= mouseDelta.y * turnSensitivity * lookImpulseStrength;
            _lookAngularVelocity.y += mouseDelta.x * turnSensitivity * lookImpulseStrength;
            _lookAngularVelocity = Vector2.ClampMagnitude(_lookAngularVelocity, maximumTurnSpeed);
        }

        public void HandleMove(Vector2 wasd) => moveInput = wasd;
        public void HandleVertical(float value) => verticalInput = value;

        private void FixedUpdate()
        {
            bool isUnderwater = UpdateWaterBoundary();
            if (!isControlled || !isUnderwater) return;

            pitch += _lookAngularVelocity.x * Time.fixedDeltaTime;
            yaw += _lookAngularVelocity.y * Time.fixedDeltaTime;
            pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
            _targetRotation = Quaternion.Euler(pitch, yaw, 0f);
            float rotationBlend = 1f - Mathf.Exp(-rotationDamping * Time.fixedDeltaTime);
            _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, _targetRotation, rotationBlend));
            _lookAngularVelocity *= Mathf.Exp(-angularCoastDamping * Time.fixedDeltaTime);

            float inputBlend = 1f - Mathf.Exp(-propulsionResponse * Time.fixedDeltaTime);
            _smoothedMoveInput = Vector2.Lerp(_smoothedMoveInput, moveInput, inputBlend);
            _smoothedVerticalInput = Mathf.Lerp(_smoothedVerticalInput, verticalInput, inputBlend);

            Vector3 propulsion = transform.forward * _smoothedMoveInput.y +
                                 transform.right * _smoothedMoveInput.x +
                                 transform.up * _smoothedVerticalInput;
            if (propulsion.y > 0f) propulsion.y *= _surfaceThrottle;
            propulsion = Vector3.ClampMagnitude(propulsion, 1f);
            if (propulsion.sqrMagnitude > .0001f)
                _rb.AddForce(propulsion * accelForce, ForceMode.Acceleration);

            float excessSpeed = _rb.linearVelocity.magnitude - moveSpeed;
            if (excessSpeed > 0f)
                _rb.AddForce(-_rb.linearVelocity.normalized * excessSpeed * softSpeedLimit,
                    ForceMode.Acceleration);
        }

        [Inject]
        public void Construct(IWaterQueryService waterQuery, IInputService input)
        {
            _waterQuery = waterQuery;
            _input = input;
            ConfigureUnderwaterPhysics();
        }

        private void ConfigureUnderwaterPhysics()
        {
            if (!_rb) return;

            if (!ignoreSurfaceForces)
            {
                if (!_buoyancy) _buoyancy = gameObject.AddComponent<BuoyancyController>();
                _buoyancy.enabled = true;
                _buoyancy.SetMode(BuoyancyMode.NeutralBuoyancy);
                if (_waterQuery != null) _buoyancy.Configure(_waterQuery);
                return;
            }

            if (_buoyancy) _buoyancy.enabled = false;
            _rb.useGravity = true;
            _rb.linearDamping = Mathf.Max(0f, airLinearDrag);
            _rb.angularDamping = Mathf.Max(0f, airAngularDrag);
        }

        private bool UpdateWaterBoundary()
        {
            _surfaceThrottle = 1f;
            if (_waterQuery == null || !_waterQuery.TrySample(_rb.worldCenterOfMass, out WaterSample sample))
            {
                ApplyAirPhysics();
                return false;
            }

            float stableSurfaceHeight = sample.WaterBody is OceanBody ocean
                ? ocean.SeaLevel
                : sample.SurfaceHeight;
            float stableDepth = stableSurfaceHeight - _rb.worldCenterOfMass.y;

            if (stableDepth <= -waterEntryBlendDistance)
            {
                ApplyAirPhysics();
                return false;
            }

            _rb.useGravity = false;
            float immersion = Mathf.InverseLerp(-waterEntryBlendDistance, minimumSubmersionDepth, stableDepth);
            immersion = Mathf.SmoothStep(0f, 1f, immersion);
            _rb.linearDamping = Mathf.Lerp(Mathf.Max(0f, airLinearDrag),
                Mathf.Max(0f, waterLinearDrag), immersion);
            _rb.angularDamping = Mathf.Lerp(Mathf.Max(0f, airAngularDrag),
                Mathf.Max(0f, waterAngularDrag), immersion);
            _rb.AddForce(Physics.gravity * (1f - immersion), ForceMode.Acceleration);

            _surfaceThrottle = Mathf.SmoothStep(0f, 1f,
                Mathf.InverseLerp(minimumSubmersionDepth,
                    minimumSubmersionDepth + surfaceBrakeDistance, stableDepth));

            if (stableDepth < minimumSubmersionDepth)
            {
                float error = minimumSubmersionDepth - stableDepth;
                float risingSpeed = Mathf.Max(0f, _rb.linearVelocity.y);
                float returnAcceleration = error * surfaceReturnStrength + risingSpeed * surfaceReturnDamping;
                _rb.AddForce(Vector3.down * returnAcceleration, ForceMode.Acceleration);
            }
            return stableDepth > 0f;
        }

        private void ApplyAirPhysics()
        {
            _rb.useGravity = true;
            _rb.linearDamping = Mathf.Max(0f, airLinearDrag);
            _rb.angularDamping = Mathf.Max(0f, airAngularDrag);
        }

        private void SubscribeInput()
        {
            if (_inputSubscribed || _input == null) return;
            _input.OnMove += HandleMove;
            _input.OnLook += HandleLook;
            _input.OnVerticalUp += HandleAscend;
            _input.OnRun += HandleDescend;
            _inputSubscribed = true;
        }

        private void UnsubscribeInput()
        {
            if (!_inputSubscribed || _input == null) return;
            _input.OnMove -= HandleMove;
            _input.OnLook -= HandleLook;
            _input.OnVerticalUp -= HandleAscend;
            _input.OnRun -= HandleDescend;
            _inputSubscribed = false;
        }

        private void HandleAscend(float value)
        {
            _ascendInput = Mathf.Clamp01(value);
            HandleVertical(_ascendInput - _descendInput);
        }

        private void HandleDescend(bool pressed)
        {
            _descendInput = pressed ? 1f : 0f;
            HandleVertical(_ascendInput - _descendInput);
        }

        private void OnDestroy() => UnsubscribeInput();
    }
}
