using AstraNope.Core;
using AstraNope.Data.Messages;
using AstraNope.Contracts;
using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace AstraNope.Services
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