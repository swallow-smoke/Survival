using System;
using _001_Scripts.Core;
using _001_Scripts.Interface;
using UnityEngine;

namespace _001_Scripts.Managers
{
    public class InputManager : Sin<InputManager>, IInputManager
    {
        public event Action onJump;
        public event Action<Vector2> onMove;
        public event Action onAttack;
        public event Action onInteract;
        public event Action onPause;
        public event Action onInventory;
        
        public void Initialize()
        {
            Debug.Log("InputManager Initialize");
            ServiceLocator.RegisterService(this);
        }

        public void Subscribe(ref Action target, Action handler)
        {
            target += handler;
        }
        
        public void UnSubscribe(ref Action target, Action handler)
        {
            target -= handler;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space)) 
                onJump?.Invoke();
            if (Input.GetKeyDown(KeyCode.Tab))
                onInventory?.Invoke();
            if (Input.GetKeyDown(KeyCode.F)) 
                onInteract?.Invoke();
            if (Input.GetKeyDown(KeyCode.Escape)) 
                onPause?.Invoke();
        }
    }
}