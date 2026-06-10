using System;
using _001_Scripts.Data.Message;
using _001_Scripts.Data.Message.Player;
using _001_Scripts.Type.States;
using MessagePipe;
using UnityEngine;
using UnityEngine.InputSystem;
using _001_Scripts.UI.Component;
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

        [SerializeField] private float maxDistance = 1f;

        [SerializeField] CameraController _camCont;

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

            moveDir = (_camCont._trs.forward * inputValue.y) + (_camCont._trs.right * inputValue.x);
            moveDir.y = 0;
            moveDir.Normalize();
            // Debug.Log(moveDir);

            if (isSwimming)
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

            Vector3 publs = transform.InverseTransformDirection(_rb.linearVelocity) / runningSpeed;

            isGround = !isSwimming && Physics.Raycast(footTrs.position, Vector3.down, maxDistance, layer);
            iMovementPublisher.Publish(new PlayerMovementMessage(_rb.linearVelocity.magnitude / runningSpeed, isGround,
                publs, isSwimming));
        }
        
        public void OnMove(InputAction.CallbackContext context)
        {
            inputValue = context.ReadValue<Vector2>();
            moveDir.y = 0;
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (!isCanMove) return;

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

        [Inject]
        public void Constructor(IPublisher<PlayerMovementMessage> movementPublisher,
            ISubscriber<PlayerStatMessage> playerStatSubscriber,
            ISubscriber<PlayerUIStateMsg> playerUIStateSubscriber)
        {
            var builder = DisposableBag.CreateBuilder();
            iMovementPublisher = movementPublisher;

            builder.Add(playerStatSubscriber.Subscribe(Stamina));
            builder.Add(playerUIStateSubscriber.Subscribe(UIState));

            _bag = builder.Build();
        }

        private void OnDestroy() => _bag?.Dispose();
    }
}