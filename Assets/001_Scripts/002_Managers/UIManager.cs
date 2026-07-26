using System;
using System.Collections.Generic;
using System.Linq;
using _001_Scripts.Base;
using _001_Scripts.Controller.Handler;
using _001_Scripts.Core;
using _001_Scripts.Data.Message;
using _001_Scripts.Data.Message.Player;
using _001_Scripts.Interface;
using _001_Scripts.Type.States;
using AYellowpaper.SerializedCollections;
using MessagePipe;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;

namespace _001_Scripts.Managers
{
    public class UIManager : MonoBehaviour, IUIService, IInitializable
    {
        private IDisposable _bag;
        private IInputService _inputServ;
        private UIPanelPresenter _panelPresenter;
        private bool _inputBound;
        private float _lastInventoryToggleTime = -10f;
        private float _lastCraftToggleTime = -10f;
        private int _lastInventoryToggleFrame = -1;
        private int _lastCraftToggleFrame = -1;
        private const float ToggleDebounceSeconds = .25f;
        [SerializedDictionary] public SerializedDictionary<string, PanelBase> uiPanels = new();

        public void Initialize()
        {
            Debug.Log("UIManager Initialize");
        }

        public void OnInvToggle()
        {
            if (_lastInventoryToggleFrame == Time.frameCount) return;
            if (Time.unscaledTime - _lastInventoryToggleTime < ToggleDebounceSeconds) return;
            _lastInventoryToggleFrame = Time.frameCount;
            _lastInventoryToggleTime = Time.unscaledTime;
            if (_panelPresenter == null)
            {
                Debug.LogError("[UIManager] Cannot toggle Inventory before UI services are ready.", this);
                return;
            }
            _panelPresenter.ToggleExclusive("Inventory", PlayerUIState.Inventory);
        }

        public void OnCraftToggle()
        {
            if (_lastCraftToggleFrame == Time.frameCount) return;
            if (Time.unscaledTime - _lastCraftToggleTime < ToggleDebounceSeconds) return;
            _lastCraftToggleFrame = Time.frameCount;
            _lastCraftToggleTime = Time.unscaledTime;
            if (_panelPresenter == null)
            {
                Debug.LogError("[UIManager] Cannot toggle Craft before UI services are ready.", this);
                return;
            }
            _panelPresenter.ToggleExclusive("Craft", PlayerUIState.Craft);
        }

        private void Start()
        {
            BindInputOnce();
        }

        private void Update()
        {
            // UI hotkeys must remain available even if PlayerInput's notification
            // behavior or VContainer initialization order changes in the scene.
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.tabKey.wasPressedThisFrame) OnInvToggle();
            if (keyboard.vKey.wasPressedThisFrame) OnCraftToggle();
        }

        private void BindInputOnce()
        {
            if (_inputBound || _inputServ == null) return;
            _inputServ.OnInventoryToggle += OnInvToggle;
            _inputServ.OnCraftToggle += OnCraftToggle;
            _inputBound = true;
        }

        [Inject]
        private void Constructor(ISubscriber<GameStateMessage> gameStateSubscriber,
            ISubscriber<UIReqMessage> _uiReqSubscriber,
            IPublisher<PlayerUIStateMsg> iUIStatePublisher,
            IInputService inputService)
        {
            var builder = DisposableBag.CreateBuilder();

            _inputServ = inputService;
            _panelPresenter = new UIPanelPresenter(uiPanels, iUIStatePublisher);
            BindInputOnce();

            gameStateSubscriber.Subscribe(OnGameStateChanged).AddTo(builder);
            _uiReqSubscriber.Subscribe(OnUIRequest).AddTo(builder);
            _bag = builder.Build();
        }

        private void OnDestroy()
        {
            if (_inputBound && _inputServ != null)
                _inputServ.OnInventoryToggle -= OnInvToggle;
            if (_inputBound && _inputServ != null)
                _inputServ.OnCraftToggle -= OnCraftToggle;
            _inputBound = false;

            _bag?.Dispose();
        }

        private void OnGameStateChanged(GameStateMessage message)
        {
            Debug.Log("Game State Changed: " + message);
        }

        private void OnUIRequest(UIReqMessage msg)
        {
            switch (msg.msgType)
            {
                case UIReqMsgType.Open:
                    _panelPresenter.OpenExclusive(msg.uiName);
                    break;
                case UIReqMsgType.Close:
                    _panelPresenter.Close(msg.uiName);
                    break;
                case UIReqMsgType.Update:
                    break;
                case UIReqMsgType.Action:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
