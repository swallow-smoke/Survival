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
        public event Action<bool> OnCrouch;
        public event Action OnJump;
        public event Action OnInteract;
        public event Action<bool> OnScanHoldChanged;
        public event Action OnExitVehicle;
        public event Action OnInventoryToggle;
        public event Action OnLogToggle;
        public event Action OnBlueprintToggle;
        public event Action<int> OnHotbarSlot;
        public event Action<float> OnHotbarScroll;
        public event Action OnSecondaryAction;

        [SerializeField, HideInInspector] private int uiBindingVersion;
        private PlayerInput _playerInput;
        private InputAction _inventoryAction;
        private InputAction _logAction;
        private InputAction _crouchAction;
        private int _lastLogToggleFrame = -1;
        private float _lastScroll;
        private bool _scanHeld;

        private void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
            _inventoryAction = _playerInput?.actions?.FindAction("Inventory", false);
            _logAction = _playerInput?.actions?.FindAction("Log", false);
            _crouchAction = _playerInput?.actions?.FindAction("Crouch", false);
            if (_inventoryAction != null)
                _inventoryAction.started += HandleInventoryToggle;
            if (_logAction != null)
                _logAction.started += HandleLogToggle;
            if (_crouchAction != null)
            {
                _crouchAction.started += HandleCrouchToggle;
                _crouchAction.canceled += HandleCrouchToggle;
            }
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.digit1Key.wasPressedThisFrame) OnHotbarSlot?.Invoke(0);
                else if (keyboard.digit2Key.wasPressedThisFrame) OnHotbarSlot?.Invoke(1);
                else if (keyboard.digit3Key.wasPressedThisFrame) OnHotbarSlot?.Invoke(2);
                else if (keyboard.digit4Key.wasPressedThisFrame) OnHotbarSlot?.Invoke(3);
                else if (keyboard.digit5Key.wasPressedThisFrame) OnHotbarSlot?.Invoke(4);
                else if (keyboard.digit6Key.wasPressedThisFrame) OnHotbarSlot?.Invoke(5);
                else if (keyboard.digit7Key.wasPressedThisFrame) OnHotbarSlot?.Invoke(6);
                else if (keyboard.digit8Key.wasPressedThisFrame) OnHotbarSlot?.Invoke(7);
                if (keyboard.bKey.wasPressedThisFrame) OnBlueprintToggle?.Invoke();
            }

            float scroll = Mouse.current?.scroll.ReadValue().y ?? 0f;
            if (Mathf.Abs(scroll) > .01f && Mathf.Abs(_lastScroll) <= .01f)
                OnHotbarScroll?.Invoke(Mathf.Sign(scroll));
            _lastScroll = scroll;

            if (Mouse.current?.rightButton.wasPressedThisFrame == true) OnSecondaryAction?.Invoke();

            bool scanHeld = Mouse.current?.rightButton.isPressed ?? false;
            if (scanHeld != _scanHeld)
            {
                _scanHeld = scanHeld;
                OnScanHoldChanged?.Invoke(_scanHeld);
            }
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
            if (ctx.performed)
            {
                OnRun?.Invoke(true);
                OnVerticalDown?.Invoke(-1f);
            }
            if (ctx.canceled)
            {
                OnRun?.Invoke(false);
                OnVerticalDown?.Invoke(0f);
            }
        }

        private void HandleCrouchToggle(InputAction.CallbackContext ctx)
        {
            if (ctx.started) OnCrouch?.Invoke(true);
            if (ctx.canceled) OnCrouch?.Invoke(false);
        }

        public void HandleInteract(InputAction.CallbackContext ctx)
        {
            if (ctx.started) OnInteract?.Invoke();
        }

        public void HandleExitVehicle(InputAction.CallbackContext ctx)
        {
            if (ctx.started) OnExitVehicle?.Invoke();
        }

        public void HandleInventoryToggle(InputAction.CallbackContext ctx)
        {
            if (ctx.started) OnInventoryToggle?.Invoke();
        }

        public void HandleLogToggle(InputAction.CallbackContext ctx)
        {
            if (!ctx.started || _lastLogToggleFrame == Time.frameCount) return;
            _lastLogToggleFrame = Time.frameCount;
            OnLogToggle?.Invoke();
        }

        private void OnDestroy()
        {
            if (_inventoryAction != null)
                _inventoryAction.started -= HandleInventoryToggle;
            if (_logAction != null)
                _logAction.started -= HandleLogToggle;
            if (_crouchAction != null)
            {
                _crouchAction.started -= HandleCrouchToggle;
                _crouchAction.canceled -= HandleCrouchToggle;
            }
        }
    }
}
