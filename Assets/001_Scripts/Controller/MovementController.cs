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
        private bool wasMove;
        private bool isGround = true;

        private IPublisher<PlayerMovementMessage> iMovementPublisher;
        private IDisposable _bag;

        [SerializeField] private float maxDistance = 1f;

        [SerializeField] CameraController _camCont;

        [SerializeField] private Transform trs;

        private void FixedUpdate()
        {
            moveDir = (_camCont._trs.forward * inputValue.y) + (_camCont._trs.right * inputValue.x);
            moveDir.y = 0;
            moveDir.Normalize();

            if (isRunning)
                _rb.linearVelocity =
                    new Vector3(moveDir.x * runningSpeed, _rb.linearVelocity.y, moveDir.z * runningSpeed);
            else _rb.linearVelocity = new Vector3(moveDir.x * speed, _rb.linearVelocity.y, moveDir.z * speed);

            if (_rb.linearVelocity.magnitude > 0)
            {
                Quaternion targetRot = Quaternion.Euler(0, trs.rotation.eulerAngles.y, 0);
                
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, 360f * Time.fixedDeltaTime);
                
                // Debug.Log($"{vector} \n {publs}");
            }
            
            Vector3 vector = new Vector3(_rb.linearVelocity.x, _rb.linearVelocity.y, _rb.linearVelocity.z);
            Vector3 publs = transform.InverseTransformDirection(_rb.linearVelocity) / runningSpeed;
            
            isGround = Physics.Raycast(transform.position, Vector3.down, maxDistance, layer);
            iMovementPublisher.Publish(new PlayerMovementMessage(vector.magnitude / runningSpeed, isRunning, isGround, publs));
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            inputValue = context.ReadValue<Vector2>();
            moveDir.y = 0;
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (context.started && isGround)
            {
                _rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
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