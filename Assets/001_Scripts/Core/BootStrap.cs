using System.Collections.Generic;
using _001_Scripts.Interface;
using _001_Scripts.Managers;
using UnityEngine;

namespace _001_Scripts.Core
{
    public class BootStrap : MonoBehaviour
    {
        [SerializeField] private List<MonoBehaviour> _managers;
        
        public void Awake()
        {
            InitializeGame();
        }

        public void InitializeGame()
        {
            
        }
    }
}