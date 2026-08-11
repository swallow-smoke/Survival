using System;
using _001_Scripts.Controller.Handler;
using _001_Scripts.Controller.Movement;
using _001_Scripts.Core._000_World._001_Water;
using _001_Scripts.Core._000_World._001_Water.Interface;
using _001_Scripts.Data.Message;
using _001_Scripts.Data.Message.Player;
using _001_Scripts.Object.Vehicle;
using _001_Scripts.Structure;
using _001_Scripts.Type.States;
using _001_Scripts.Vehicle.Component;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace _001_Scripts.Controller
{
    [RequireComponent(typeof(Rigidbody))]
    public class MovementController : MonoBehaviour
    {
        [SerializeField] private float speed = 15.0f;
        [SerializeField] private float runningSpeed = 35.0f;
        [SerializeField] private float jumpForce = 5.0f;
        [SerializeField] private float crouchSpeed = 7.5f;
        [SerializeField] private float swimSpeed = 10.0f;
        [SerializeField] private float swimVerticalSpeed = 5.0f;
        [SerializeField] private Rigidbody _rb;
        [SerializeField] private LayerMask layer;
        [SerializeField] private Transform footTrs;
        [SerializeField] private float maxDistance = 1f;
        [SerializeField] private CameraController _camCont;
        private IWaterQueryService _waterQuery;
        private PlayerWaterSensor _waterSensor;
        private UnderwaterVolumeController _underwaterVolume;

        private Vector2 inputValue;
        private bool isRunning;
        private bool isGround = true;
        private bool isCanMove = true;
        private bool isSwimming;
        private bool isSwimUp;
        private bool isSwimDown;
        private bool isCrouching;

        private MovementContext _ctx;
        private IMovementMode _ground;
        private IMovementMode _swim;
        private IMovementMode _insideLarge;

        private IPublisher<PlayerMovementMessage> iMovementPublisher;
        private IInputService _input;
        private IDisposable _bag;
        private Action<PlayerWaterState> _waterStateHandler;

        private IVehicleControllable _activeVehicle;
        private SeatComponent _activeSeat;
        private PlayerVehicleState _vehicleState;

        private void Awake()
        {
            if (!_rb) _rb = GetComponent<Rigidbody>();
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            EnsureWaterComponents();

            _ctx = new MovementContext
            {
                Rb = _rb,
                Speed = speed,
                RunningSpeed = runningSpeed,
                CrouchSpeed = crouchSpeed,
                SwimSpeed = swimSpeed,
                SwimVerticalSpeed = swimVerticalSpeed,
            };
            _ground = new GroundMovementMode();
            _swim = new SwimMovementMode();
            _insideLarge = new InsideLargeMovementMode();
        }

        private void Start()
        {
            if (_input == null) return;

            _input.OnMove += HandleMove;
            _input.OnLook += HandleLook;
            _input.OnVerticalUp += HandleVerticalUp;
            _input.OnJump += HandleJump;
            _input.OnRun += HandleRun;
            _input.OnVerticalDown += HandleVerticalDown;
            _input.OnExitVehicle += HandleExitVehicle;
        }

        private void FixedUpdate()
        {
            // VContainer may inject one frame later while scripts are reloaded in the Editor.
            // Do not let an incomplete dependency graph stop Play Mode or flood the Console.
            if (_ctx == null || !_rb || !_camCont || !footTrs ||
                _waterQuery == null || iMovementPublisher == null) return;
            if (!isCanMove) return;
            if (_vehicleState == PlayerVehicleState.Seated) return;

            PlayerWaterState waterState = _waterSensor.SampleNow();
            if (waterState.Swimming != isSwimming) SetSwimming(waterState.Swimming);

            _ctx.MoveDir = ComputeMoveDir();
            _ctx.IsRunning = isRunning;
            _ctx.IsCrouching = isCrouching;
            _ctx.IsSwimUp = isSwimUp;
            _ctx.IsSwimDown = isSwimDown;
            
            SelectMode().Tick(_ctx);
            
            isGround = !isSwimming && Physics.Raycast(footTrs.position, Vector3.down, maxDistance, layer);
            Vector3 publs = transform.InverseTransformDirection(_rb.linearVelocity) / runningSpeed;
            iMovementPublisher.Publish(new PlayerMovementMessage(_rb.linearVelocity.magnitude / runningSpeed, isGround, publs, isSwimming));
        }

        private IMovementMode SelectMode()
        {
            if (_vehicleState == PlayerVehicleState.InsideLarge) return _insideLarge;
            if (isSwimming) return _swim;
            return _ground;
        }

        private Vector3 ComputeMoveDir()
        {
            Vector3 dir = (_camCont.PlanarForward * inputValue.y) + (_camCont.PlanarRight * inputValue.x);
            dir.y = 0;
            dir.Normalize();
            return dir;
        }

        private void HandleMove(Vector2 value)
        {
            inputValue = value;

            if (_activeVehicle != null)
                _activeVehicle.HandleMove(inputValue);
        }

        private void HandleLook(Vector2 value)
        {
            if (_activeVehicle is SmallSubVehicle small)
                small.HandleLook(value);
        }

        private void HandleVerticalUp(float value)
        {
            if (!isCanMove) return;

            if (_activeVehicle != null)
            {
                _activeVehicle.HandleVertical(value);
                return;
            }

            if (isSwimming)
                isSwimUp = value > 0f;
        }

        private void HandleJump()
        {
            if (!isCanMove) return;
            if (_activeVehicle != null) return;
            if (isSwimming) return;

            if (isGround)
                _rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        private void HandleRun(bool value)
        {
            isRunning = value;
        }

        private void HandleVerticalDown(float value)
        {
            if (_activeVehicle != null)
            {
                _activeVehicle.HandleVertical(value);
                return;
            }

            if (isSwimming)
                isSwimDown = value < 0f;
            else
                isCrouching = value < 0f;
        }

        private void SetSwimming(bool value)
        {
            isSwimming = value;
            _rb.useGravity = !value;
            if (!value)
            {
                isSwimUp = false;
                isSwimDown = false;
                isRunning = false;
                isCrouching = false;
            }
        }

        public void Stamina(PlayerStatMessage msg)
        {
            if (msg.stamina <= 0)
                isRunning = false;
        }

        private void UIState(PlayerUIStateMsg msg)
        {
            switch (msg.state)
            {
                case PlayerUIState.Inventory:
                case PlayerUIState.Craft:
                case PlayerUIState.Workbench:
                case PlayerUIState.SubmarineFabricator:
                    isCanMove = false;
                    break;
                default:
                    isCanMove = true;
                    break;
            }
        }

        private void OnVehicleControlAssigned(VehicleControlAssignedMsg msg)
        {
            _activeVehicle = msg.Controller;
            _activeSeat = msg.Seat as SeatComponent;
        }

        private void HandleExitVehicle()
        {
            if (_vehicleState != PlayerVehicleState.Seated) return;
            _activeSeat?.StandWithDefaults();
        }

        private void OnVehicleStateChanged(PlayerVehicleStateMsg msg)
        {
            _vehicleState = msg.state;
            _rb.isKinematic = (msg.state != PlayerVehicleState.None);
            if (msg.state == PlayerVehicleState.None)
                _rb.useGravity = !isSwimming;
        }

        [Inject]
        public void Constructor(IPublisher<PlayerMovementMessage> movementPublisher,
            ISubscriber<PlayerStatMessage> playerStatSubscriber,
            ISubscriber<PlayerUIStateMsg> playerUIStateSubscriber,
            ISubscriber<PlayerVehicleStateMsg> vehicleStateSubscriber,
            ISubscriber<VehicleControlAssignedMsg> vehicleControlSubscriber,
            IInputService inputService,
            IWaterQueryService waterQuery,
            IPublisher<PlayerWaterStateMessage> waterStatePublisher)
        {
            var builder = DisposableBag.CreateBuilder();
            iMovementPublisher = movementPublisher;
            _waterQuery = waterQuery;
            _input = inputService;
            EnsureWaterComponents();
            _waterSensor.Configure(waterQuery, footTrs, transform, _camCont != null ? _camCont.ViewTransform : null,
                _camCont != null ? _camCont.ViewTransform : null);
            _waterStateHandler = state => waterStatePublisher.Publish(new PlayerWaterStateMessage(state));
            _waterSensor.StateChanged += _waterStateHandler;
            _underwaterVolume.Configure(_waterSensor);

            builder.Add(playerStatSubscriber.Subscribe(Stamina));
            builder.Add(playerUIStateSubscriber.Subscribe(UIState));
            builder.Add(vehicleStateSubscriber.Subscribe(OnVehicleStateChanged));
            builder.Add(vehicleControlSubscriber.Subscribe(OnVehicleControlAssigned));

            _bag = builder.Build();
        }

        private void EnsureWaterComponents()
        {
            _waterSensor ??= GetComponent<PlayerWaterSensor>();
            if (_waterSensor == null) _waterSensor = gameObject.AddComponent<PlayerWaterSensor>();
            _underwaterVolume ??= GetComponent<UnderwaterVolumeController>();
            if (_underwaterVolume == null) _underwaterVolume = gameObject.AddComponent<UnderwaterVolumeController>();
        }

        private void OnDestroy()
        {
            if (_input != null)
            {
                _input.OnMove -= HandleMove;
                _input.OnLook -= HandleLook;
                _input.OnVerticalUp -= HandleVerticalUp;
                _input.OnJump -= HandleJump;
                _input.OnRun -= HandleRun;
                _input.OnVerticalDown -= HandleVerticalDown;
                _input.OnExitVehicle -= HandleExitVehicle;
            }

            _bag?.Dispose();
            if (_waterSensor != null && _waterStateHandler != null)
                _waterSensor.StateChanged -= _waterStateHandler;
        }
    }
}
