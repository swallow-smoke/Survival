using System;
using _001_Scripts.Core;
using _001_Scripts.Interface;
using _001_Scripts.Managers;
using UnityEngine;
using EventType = _001_Scripts.Type.EventType;

namespace _001_Scripts.Controller
{
    [RequireComponent(typeof(InventoryController))]
    public class PlayerController : MonoBehaviour
    {
        private Animator _animator;
        
        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        public void OnInteract()
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, transform.forward, out hit, 2.0f))
            {
                Debug.Log("Interacted with: " + hit.collider.name);
            }
        }
    }
}