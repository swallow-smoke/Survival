using System;
using _001_Scripts.Data.Message;
using _001_Scripts.UI.Component;
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
        [SerializeField] private Rigidbody _rb;
        [SerializeField] private LayerMask layer;
        private Vector3 moveDir;
        private Vector2 inputValue;
        private bool isCanJump;
        private bool isRunning;

        private IPublisher<PlayerMovementMessage> iMovementPublisher;
        private IDisposable _bag;

        [SerializeField] private float maxDistance = 1f;

        [SerializeField] CameraController _camCont;

        private void FixedUpdate()
        {
            moveDir = (_camCont._trs.forward * inputValue.y) + (_camCont._trs.right * inputValue.x);
            moveDir.y = 0;
            moveDir.Normalize();

            if (isRunning)
                _rb.linearVelocity =
                    new Vector3(moveDir.x * runningSpeed, _rb.linearVelocity.y, moveDir.z * runningSpeed);
            else _rb.linearVelocity = new Vector3(moveDir.x * speed, _rb.linearVelocity.y, moveDir.z * speed);
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, maxDistance, layer))
            {
                isCanJump = true;
            }
            else isCanJump = false;

            if (_rb.linearVelocity.magnitude > 0)
            {
                iMovementPublisher.Publish(new PlayerMovementMessage(_rb.linearVelocity.magnitude, isRunning));
            }
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            inputValue = context.ReadValue<Vector2>();
            moveDir.y = 0;
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (context.started && isCanJump)
            {
                _rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                isCanJump = false;
            }
        }

        public void OnRunning(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                isRunning = true;
            }
            else if (context.canceled)
                isRunning = false;
        }

        [Inject]
        public void Constructor(IPublisher<PlayerMovementMessage> movementPublisher,
            ISubscriber<ForceWalkMessage> forceWalkSubscriber)
        {
            var builder = DisposableBag.CreateBuilder();
            iMovementPublisher = movementPublisher;

            builder.Add(forceWalkSubscriber.Subscribe(SetRun));

            _bag = builder.Build();
        }

        private void SetRun(ForceWalkMessage msg) => isRunning = false;

        private void OnDestroy() => _bag?.Dispose();
    }
}