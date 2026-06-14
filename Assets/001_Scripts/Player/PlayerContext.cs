using _001_Scripts.Interface;
using UnityEngine;

namespace _001_Scripts.Structure
{
    public class PlayerContext : MonoBehaviour, IPlayerContext
    {
        [SerializeField] private Transform playerTrs;
        public Transform PlayerTrs => playerTrs;
    }
}
