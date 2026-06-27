using _001_Scripts.Core._000_World._001_Water.Interface;
using UnityEngine;

namespace _001_Scripts.Core._000_World._001_Water
{
    public class OceanBody : MonoBehaviour, IWaterbody
    {
        [SerializeField] private float seaLevel;

        public float GetSurfaceY(Vector3 position) => seaLevel;
        public bool Contain(Vector3 position) => true;
    }
}