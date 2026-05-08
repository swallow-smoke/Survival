using System;
using _001_Scripts.Controller;
using _001_Scripts.Managers;
using _001_Scripts.Type;
using UnityEngine;
using EventType = _001_Scripts.Type.EventType;

namespace _001_Scripts.Interface
{
    public interface IInputService
    {
        void AddKeyDownListener(CustomKeyCode keyCode, Action action);
        void AddKeyUpListener(CustomKeyCode keyCode, Action action);
        void AddKeyListener(CustomKeyCode keyCode, Action action);
        
        void RemoveKeyDownListener(CustomKeyCode keyCode, Action action);
        void RemoveKeyUpListener(CustomKeyCode keyCode, Action action);
        void RemoveKeyListener(CustomKeyCode keyCode, Action action);
        
        public MovementHandler MovementHandler { get; }
    }
}