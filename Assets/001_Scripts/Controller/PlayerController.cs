using System;
using _001_Scripts.Core;
using _001_Scripts.Interface;
using _001_Scripts.Managers;
using UnityEngine;
using EventType = _001_Scripts.Type.EventType;

namespace _001_Scripts.Controller
{
    [RequireComponent(typeof(Rigidbody), typeof(InventoryController))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] public float speed = 15.0f;
        [SerializeField] public float jumpForce = 5.0f;
        private Rigidbody _rigidbody;
        private Animator _animator;

        private IInputService input;
        
        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _animator = GetComponent<Animator>();
        }

        private void FixedUpdate()
        {
            OnMovement();
        }

        private void OnEnable()
        {
            input = ServiceLocator.GetService<InputManager>();
            
            
        }

        public void OnJump()
        {
            _rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        public void OnInteract()
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, transform.forward, out hit, 2.0f))
            {
                Debug.Log("Interacted with: " + hit.collider.name);
            }
        }

        public void OnInventory()
        {
            
        }

        public void OnMovement()
        {
            if (input == null) return;
            
            var dir = input.MovementHandler.Dir;
            
            var velocity = new Vector3(dir.x, 0, dir.y) * speed;
            _rigidbody.velocity = velocity;
        }
    }
}