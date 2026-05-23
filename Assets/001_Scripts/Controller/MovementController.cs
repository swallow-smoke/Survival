using System;
using _001_Scripts.Data.Message;
using MessagePipe;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace _001_Scripts.Controller
{
    [RequireComponent(typeof(Rigidbody))]
    public class MovementController : MonoBehaviour
    {
        [SerializeField] public float speed = 15.0f;
        [SerializeField] public float jumpForce = 5.0f;
        [SerializeField] private Rigidbody _rb;
        [SerializeField] private LayerMask layer;
        private Vector3 moveDir;
        private Vector2 inputValue;
        private bool isCanJump;

        private IPublisher<PlayerMovementMessage> iMovementPublisher;

        [SerializeField] private float maxDistance = 1f;

        [SerializeField] CameraController _camCont;

        private void FixedUpdate()
        {
            moveDir = (_camCont._trs.forward * inputValue.y) + (_camCont._trs.right * inputValue.x);
            moveDir.y = 0;
            moveDir.Normalize();

            _rb.linearVelocity = new Vector3(moveDir.x * speed, _rb.linearVelocity.y, moveDir.z * speed);
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, maxDistance,layer))
            {
                isCanJump = true;
            }

            if (_rb.linearVelocity.magnitude > 0)
            {
                iMovementPublisher.Publish(new PlayerMovementMessage());
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

        [Inject]
        public void Constructor(IPublisher<PlayerMovementMessage> movementPublisher)
        {
            iMovementPublisher = movementPublisher;
        }
    }
}