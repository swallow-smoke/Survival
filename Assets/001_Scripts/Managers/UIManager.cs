using _001_Scripts.Core;
using _001_Scripts.Interface;
using UnityEngine;

namespace _001_Scripts.Managers
{
    public class UIManager : Sin<UIManager>, IUIService, IManager
    {
        public void Initialize()
        {
            Debug.Log("UIManager Initialize");
            ServiceLocator.RegisterService(this);
        }
    }
}