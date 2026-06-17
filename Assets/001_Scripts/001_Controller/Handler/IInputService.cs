using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _001_Scripts.Controller.Handler
{
    public interface IInputService
    {
        event Action<Vector2> OnMove;
        event Action<Vector2> OnLook;
        event Action<float> OnVerticalUp;
        event Action<float> OnVerticalDown;
        event Action<bool> OnRun;
        event Action OnJump;
        event Action OnInteract;
        event Action OnExitVehicle;
        event Action<float> OnPersonChange;
        event Action OnInventoryToggle;
    }
}