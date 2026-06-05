using System;
using System.Collections.Generic;
using System.Linq;
using _001_Scripts.Base;
using _001_Scripts.Core;
using _001_Scripts.Data.Message;
using _001_Scripts.Interface;
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
        [SerializedDictionary] public SerializedDictionary<string, PanelBase> uiPanels = new();

        public void Initialize()
        {
            Debug.Log("UIManager Initialize");
        }

        public void OnInvToggle(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                var panel = uiPanels["Inventory"];

                if (panel.isViz)
                    panel.Close();
                else
                    panel.Open();
            }
        }

        [Inject]
        private void Constructor(ISubscriber<GameStateMessage> gameStateSubscriber,
            ISubscriber<UIReqMessage> _uiReqSubscriber)
        {
            var builder = DisposableBag.CreateBuilder();

            gameStateSubscriber.Subscribe(OnGameStateChanged).AddTo(builder);
            _uiReqSubscriber.Subscribe(OnUIRequest).AddTo(builder);
            _bag = builder.Build();
        }

        private void OnDestroy()
        {
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