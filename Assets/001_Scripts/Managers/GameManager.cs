using _001_Scripts.Core;
using _001_Scripts.Interface;
using UnityEngine;

namespace _001_Scripts.Managers
{
    public class GameManager : Sin<GameManager>, IGameService, IManager
    {
        public void Initialize()
        {
            Debug.Log("GameManager Initialize");
            ServiceLocator.RegisterService(this);
        }
    }
}