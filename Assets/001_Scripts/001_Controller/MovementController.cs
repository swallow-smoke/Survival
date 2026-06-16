using System;
using _001_Scripts.Data.Message;
using _001_Scripts.Data.Message.Player;
using _001_Scripts.Data.Structure.Interface;
using _001_Scripts.Structure;
using _001_Scripts.Type.States;
using MessagePipe;
using UnityEngine;
using UnityEngine.InputSystem;
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
        [SerializeField] private LayerMask waterLayer;
        [SerializeField] private Rigidbody _rb;
        [SerializeField] private LayerMask layer;
        [SerializeField] private Transform footTrs;
        [SerializeField] private Transform _trs;
        private Vector3 moveDir;
        private Vector2 inputValue;
        private bool isRunning;
        private bool isGround = true;
        private bool isCanMove = true;
        private bool isSwimming;
        private bool isSwimUp;
        private bool isSwimDown;
        private bool isCrouching;

        private IPublisher<PlayerMovementMessage> iMovementPublisher;
        private IDisposable _bag;

        private IVehicleControllable _activeVehicle;
        private SeatComponent _activeSeat;
        private PlayerVehicleState _vehicleState;

        [SerializeField] private float maxDistance = 1f;
        [SerializeField] CameraController _camCont;

        private void Awake()
        {
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        private void OnTriggerEnter(Collider other)
        {
            if ((waterLayer.value & (1 << other.gameObject.layer)) != 0)
                SetSwimming(true);
        }

        private void OnTriggerExit(Collider other)
        {
            if ((waterLayer.value & (1 << other.gameObject.layer)) != 0)
                SetSwimming(false);
        }

        private void FixedUpdate()
        {
            if (!isCanMove) return;

            if (_vehicleState == PlayerVehicleState.Seated) return;

            moveDir = (_camCont.transform.forward * inputValue.y) + (_camCont.transform.right * inputValue.x);
            moveDir.y = 0;
            moveDir.Normalize();

            if (_vehicleState == PlayerVehicleState.InsideLarge)
            {
                float currentSpeed = isCrouching ? crouchSpeed : (isRunning ? runningSpeed : speed);
                _rb.MovePosition(_rb.position + moveDir * (currentSpeed * Time.fixedDeltaTime));
            }
            else if (isSwimming)
            {
                float verticalVelocity = 0f;
                if (isSwimUp) verticalVelocity = swimVerticalSpeed;
                else if (isSwimDown) verticalVelocity = -swimVerticalSpeed;

                _rb.linearVelocity = new Vector3(moveDir.x * swimSpeed, verticalVelocity, moveDir.z * swimSpeed);
            }
            else
            {
                float currentSpeed = isCrouching ? crouchSpeed : (isRunning ? runningSpeed : speed);
                _rb.linearVelocity = new Vector3(moveDir.x * currentSpeed, _rb.linearVelocity.y, moveDir.z * currentSpeed);
            }

            if (_rb.linearVelocity.magnitude > 0.1f)
            {
                Quaternion targetRot = Quaternion.Euler(0, _trs.rotation.eulerAngles.y, 0);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, 360f * Time.fixedDeltaTime);
            }

            Vector3 publs = transform.InverseTransformDirection(_rb.linearVelocity) / runningSpeed;
            isGround = !isSwimming && Physics.Raycast(footTrs.position, Vector3.down, maxDistance, layer);
            iMovementPublisher.Publish(new PlayerMovementMessage(_rb.linearVelocity.magnitude / runningSpeed, isGround, publs, isSwimming));
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            inputValue = context.ReadValue<Vector2>();

            if (_activeVehicle != null)
            {
                _activeVehicle.HandleMove(inputValue);
                return;
            }

            moveDir.y = 0;
        }

        public void OnLook(InputAction.CallbackContext context)
        {
            if (_activeVehicle is SmallSubVehicle small)
                small.HandleLook(context.ReadValue<Vector2>());
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (!isCanMove) return;

            if (_activeVehicle != null)
            {
                if (context.started) _activeVehicle.HandleVertical(1f);
                else if (context.canceled) _activeVehicle.HandleVertical(0f);
                return;
            }

            if (isSwimming)
            {
                if (context.started) isSwimUp = true;
                else if (context.canceled) isSwimUp = false;
            }
            else if (context.started && isGround)
            {
                _rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            }
        }

        public void OnRunning(InputAction.CallbackContext context)
        {
            if (context.performed) isRunning = true;
            else if (context.canceled) isRunning = false;
        }

        public void OnShift(InputAction.CallbackContext context)
        {
            if (_activeVehicle != null)
            {
                if (context.started) _activeVehicle.HandleVertical(-1f);
                else if (context.canceled) _activeVehicle.HandleVertical(0f);
                return;
            }

            if (isSwimming)
            {
                if (context.started) isSwimDown = true;
                else if (context.canceled) isSwimDown = false;
            }
            else
            {
                if (context.started) isCrouching = true;
                else if (context.canceled) isCrouching = false;
            }
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

        public void OnExitVehicle(InputAction.CallbackContext context)
        {
            if (!context.started) return;
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
            ISubscriber<VehicleControlAssignedMsg> vehicleControlSubscriber)
        {
            var builder = DisposableBag.CreateBuilder();
            iMovementPublisher = movementPublisher;

            builder.Add(playerStatSubscriber.Subscribe(Stamina));
            builder.Add(playerUIStateSubscriber.Subscribe(UIState));
            builder.Add(vehicleStateSubscriber.Subscribe(OnVehicleStateChanged));
            builder.Add(vehicleControlSubscriber.Subscribe(OnVehicleControlAssigned));

            _bag = builder.Build();
        }

        private void OnDestroy() => _bag?.Dispose();
    }
}
