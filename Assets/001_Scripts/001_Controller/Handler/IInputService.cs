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
        event Action<bool> OnCrouch;
        event Action OnJump;
    }

    public interface IInteractionInput
    {
        event Action OnInteract;
        event Action<bool> OnScanHoldChanged;
    }

    public interface IVehicleInput
    {
        event Action OnExitVehicle;
    }

    public interface IUIInput
    {
        event Action OnInventoryToggle;
        event Action OnLogToggle;
        event Action OnBlueprintToggle;
    }

    public interface IHotbarInput
    {
        event Action<int> OnHotbarSlot;
        event Action<float> OnHotbarScroll;
    }

    public interface IHeldItemInput
    {
        event Action OnSecondaryAction;
    }

    public interface IInputService : IMovementInput, IInteractionInput, IVehicleInput, IUIInput, IHotbarInput,
        IHeldItemInput { }
}
