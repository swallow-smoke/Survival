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
        [SerializedDictionary] public SerializedDictionary<string, PanelBase> uiPanels = new();

        public void Initialize()
        {
            Debug.Log("UIManager Initialize");
        }

        public void OnInvToggle()
            => _panelPresenter.Toggle("Inventory", PlayerUIState.Inventory);

        private void Start()
        {
            if (_inputServ == null) return;

            _inputServ.OnInventoryToggle += OnInvToggle;
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

            gameStateSubscriber.Subscribe(OnGameStateChanged).AddTo(builder);
            _uiReqSubscriber.Subscribe(OnUIRequest).AddTo(builder);
            _bag = builder.Build();
        }

        private void OnDestroy()
        {
            if (_inputServ != null)
                _inputServ.OnInventoryToggle -= OnInvToggle;

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
                    break;
                case UIReqMsgType.Close:
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