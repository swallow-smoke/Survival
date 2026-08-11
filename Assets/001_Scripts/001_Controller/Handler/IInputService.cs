using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _001_Scripts.Controller.Handler
{
    public interface IMovementInput
    {
        event Action<Vector2> OnMove;
        event Action<Vector2> OnLook;
        event Action<float> OnVerticalUp;
        event Action<float> OnVerticalDown;
        event Action<bool> OnRun;
        event Action OnJump;
    }

    public interface IInteractionInput
    {
        event Action OnInteract;
    }

    public interface IVehicleInput
    {
        event Action OnExitVehicle;
        event Action<float> OnPersonChange;
    }

    public interface IUIInput
    {
        event Action OnInventoryToggle;
        event Action OnCraftToggle;
    }

    public interface IInputService : IMovementInput, IInteractionInput, IVehicleInput, IUIInput { }
}
