using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _001_Scripts.Controller
{
    [RequireComponent(typeof(Rigidbody))]
    public class MovementController : MonoBehaviour
    {
        [SerializeField] public float speed = 15.0f;
        [SerializeField] public float jumpForce = 5.0f;
        [SerializeField] private Rigidbody _rb;
        private Vector3 moveDir;
        private Vector2 inputValue;
        
        [SerializeField] CameraController _camCont;
        
        private void FixedUpdate()
        {
            Vector3 moveVector = new Vector3(moveDir.x, _rb.linearVelocity.y, moveDir.z);
            _rb.linearVelocity = moveVector.normalized * speed;
            moveDir = (_camCont._trs.forward * inputValue.y) + (_camCont._trs.right * inputValue.x);
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            inputValue = context.ReadValue<Vector2>();

            
            moveDir.y = 0;
            moveDir.Normalize();
        }
    }
}