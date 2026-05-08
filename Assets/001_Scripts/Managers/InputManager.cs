using System;
using System.Collections.Generic;
using _001_Scripts.Controller;
using _001_Scripts.Core;
using _001_Scripts.Interface;
using _001_Scripts.Type;
using UnityEngine;

namespace _001_Scripts.Managers
{
    public class InputManager : Sin<InputManager>, IInputService, IManager
    {
        public MovementHandler MovementHandler { get; private set; }
            
        private static readonly CustomKeyCode[] keyCodes =
        {
            CustomKeyCode.Jump,
            CustomKeyCode.Attack,
            CustomKeyCode.Dash,
            CustomKeyCode.Inventory,
        };
        
        private readonly Dictionary<CustomKeyCode, Action> keyDownHandlers = new();
        private readonly Dictionary<CustomKeyCode, Action> keyUpHandlers = new();
        private readonly Dictionary<CustomKeyCode, Action> keyHandlers = new();
        private event Action<Vector2> onMove;
 
        public void Initialize()
        {
            Debug.Log("InputManager Initialize");
            ServiceLocator.RegisterService(this);
            
            MovementHandler = new MovementHandler();
        }

        private void Update()
        {
            for (var i = 0; i < keyCodes.Length; ++i)
            {
                var keyCode = keyCodes[i];
                var unityKeyCode = (KeyCode)keyCode;

                if (Input.GetKeyDown(unityKeyCode))
                    InvokeKeyDown(keyCode);
                
                if (Input.GetKey(unityKeyCode))
                    InvokeKey(keyCode);

                if (Input.GetKeyUp(unityKeyCode))
                    InvokeKeyUp(keyCode);
                
            }
            
            MovementHandler.Update();
        }

        public void AddKeyDownListener(CustomKeyCode keyCode, Action action)
        {
            if (keyDownHandlers.TryGetValue(keyCode, out var handler))
            {
                keyDownHandlers[keyCode] = handler + action;
            }
            else
            {
                keyDownHandlers[keyCode] = action;
            }
        }

        public void RemoveKeyDownListener(CustomKeyCode keyCode, Action action)
        {
            if (keyDownHandlers.TryGetValue(keyCode, out var handler))
            {
                handler -= action;

                if (handler == null)
                {
                    keyDownHandlers.Remove(keyCode);
                }
                else
                {
                    keyDownHandlers[keyCode] = handler;
                }
            }
        }

        public void AddKeyListener(CustomKeyCode keyCode, Action action)
        {
            if (keyHandlers.TryGetValue(keyCode, out var handler))
            {
                keyHandlers[keyCode] = handler + action;
            }
            else
            {
                keyHandlers[keyCode] = action;
            }
        }

        public void RemoveKeyListener(CustomKeyCode keyCode, Action action)
        {
            if (keyHandlers.TryGetValue(keyCode, out var handler))
            {
                handler -= action;

                if (handler == null)
                {
                    keyHandlers.Remove(keyCode);
                }
                else
                {
                    keyHandlers[keyCode] = handler;
                }
            }
        }

        public void AddKeyUpListener(CustomKeyCode keyCode, Action action)
        {
            if (keyUpHandlers.TryGetValue(keyCode, out var handler))
            {
                keyUpHandlers[keyCode] = handler + action;
            }
            else
            {
                keyUpHandlers[keyCode] = action;
            }
        }

        public void RemoveKeyUpListener(CustomKeyCode keyCode, Action action)
        {
            if (keyUpHandlers.TryGetValue(keyCode, out var handler))
            {
                handler -= action;

                if (handler == null)
                {
                    keyUpHandlers.Remove(keyCode);
                }
                else
                {
                    keyUpHandlers[keyCode] = handler;
                }
            }
        }

        private void InvokeKeyDown(CustomKeyCode keyCode)
        {
            if (keyDownHandlers.TryGetValue(keyCode, out var handler))
            {
                handler?.Invoke();
            }
        }

        private void InvokeKey(CustomKeyCode keyCode)
        {
            if (keyHandlers.TryGetValue(keyCode, out var handler))
            {
                handler?.Invoke();
            }
        }

        private void InvokeKeyUp(CustomKeyCode keyCode)
        {
            if (keyUpHandlers.TryGetValue(keyCode, out var handler))
            {
                handler?.Invoke();
            }
        }
    }
}