using System.Collections.Generic;
using _001_Scripts.Interface;
using _001_Scripts.Managers;
using UnityEngine;

namespace _001_Scripts.Core
{
    public class BootStrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        public static void Boot()
        {
            InitializeGame();
        }

        public static void InitializeGame()
        {
        //     ServiceLocator.RegisterService(GameManager.Instance);
        //     ServiceLocator.RegisterService(UIManager.Instance);
        }
    }
}