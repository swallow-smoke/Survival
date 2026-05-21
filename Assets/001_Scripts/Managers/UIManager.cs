using System;
using _001_Scripts.Core;
using _001_Scripts.Data.Message;
using _001_Scripts.Interface;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace _001_Scripts.Managers
{
    public class UIManager : MonoBehaviour, IUIService, IManager
    {
        private IDisposable _bag;

        public void Initialize()
        {
            Debug.Log("UIManager Initialize");
        }

        [Inject]
        private void Constructor(ISubscriber<GameStateMessage> gameStateSubscriber)
        {
            _bag = DisposableBag.Create(
                gameStateSubscriber.Subscribe(OnGameStateChanged)
                );
        }

        private void OnDestroy()
        {
            _bag.Dispose();
        }

        private void OnGameStateChanged(GameStateMessage message)
        {
            Debug.Log("Game State Changed: " + message);
        }
    }
}