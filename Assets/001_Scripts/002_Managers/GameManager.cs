using _001_Scripts.Core;
using _001_Scripts.Data.Message;
using _001_Scripts.Interface;
using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace _001_Scripts.Managers
{
    public class GameManager : MonoBehaviour, IGameService, IInitializable
    {
        IPublisher<GameStateMessage> _gameStatePublisher;
        
        public void Initialize()
        {
            Debug.Log("GameManager Initialize");
        }

        [Inject]
        private void Constructor(IPublisher<GameStateMessage> gameStatePublisher)
        {
            _gameStatePublisher = gameStatePublisher;
        }
    }
}