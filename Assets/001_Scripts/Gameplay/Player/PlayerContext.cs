using AstraNope.Contracts;
using UnityEngine;

namespace AstraNope.Gameplay.Player
{
    public class PlayerContext : MonoBehaviour, IPlayerContext
    {
        [SerializeField] private Transform playerTrs;
        public Transform PlayerTrs => playerTrs;
    }
}
