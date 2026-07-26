using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _001_Scripts.Controller.Handler
{
    public class InputHandler : MonoBehaviour, IInputService
    {
        public event Action<Vector2> OnMove;
        public event Action<Vector2> OnLook;
        public event Action<float> OnVerticalUp;
        public event Action<float> OnVerticalDown;
        public event Action<bool> OnRun;
        public event Action OnJump;
        public event Action OnInteract;
        public event Action OnExitVehicle;
        public event Action<float> OnPersonChange;
        public event Action OnInventoryToggle;
        public event Action OnCraftToggle;

        [SerializeField, HideInInspector] private int uiBindingVersion;
        private PlayerInput _playerInput;
        private InputAction _inventoryAction;
        private InputAction _craftAction;
        private int _lastCraftToggleFrame = -1;

        private void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
            _inventoryAction = _playerInput?.actions?.FindAction("Inventory", false);
            _craftAction = _playerInput?.actions?.FindAction("Craft", false);
            if (_inventoryAction != null)
                _inventoryAction.started += HandleInventoryToggle;
            if (_craftAction != null)
                _craftAction.started += HandleCraftToggle;
        }

        public void HandleMove(InputAction.CallbackContext ctx)
            => OnMove?.Invoke(ctx.ReadValue<Vector2>());

        public void HandleLook(InputAction.CallbackContext ctx)
            => OnLook?.Invoke(ctx.ReadValue<Vector2>());

        public void HandleJump(InputAction.CallbackContext ctx)
        {
            if (ctx.started) OnVerticalUp?.Invoke(1f);
            if (ctx.canceled) OnVerticalUp?.Invoke(0f);
            if (ctx.started) OnJump?.Invoke();
        }

        public void HandleShift(InputAction.CallbackContext ctx)
        {
            if (ctx.started) OnVerticalDown?.Invoke(-1f);
            if (ctx.canceled) OnVerticalDown?.Invoke(0f);
        }

        public void HandleRun(InputAction.CallbackContext ctx)
        {
            if (ctx.performed) OnRun?.Invoke(true);
            if (ctx.canceled) OnRun?.Invoke(false);
        }

        public void HandleInteract(InputAction.CallbackContext ctx)
        {
            if (ctx.started) OnInteract?.Invoke();
        }

        public void HandleExitVehicle(InputAction.CallbackContext ctx)
        {
            if (ctx.started) OnExitVehicle?.Invoke();
        }

        public void HandlePersonChange(InputAction.CallbackContext ctx)
        {
            if (ctx.performed) OnPersonChange?.Invoke(ctx.ReadValue<float>());
        }

        public void HandleInventoryToggle(InputAction.CallbackContext ctx)
        {
            if (ctx.started) OnInventoryToggle?.Invoke();
        }

        public void HandleCraftToggle(InputAction.CallbackContext ctx)
        {
            if (!ctx.started || _lastCraftToggleFrame == Time.frameCount) return;
            _lastCraftToggleFrame = Time.frameCount;
            OnCraftToggle?.Invoke();
        }

        private void OnDestroy()
        {
            if (_inventoryAction != null)
                _inventoryAction.started -= HandleInventoryToggle;
            if (_craftAction != null)
                _craftAction.started -= HandleCraftToggle;
        }
    }
}
