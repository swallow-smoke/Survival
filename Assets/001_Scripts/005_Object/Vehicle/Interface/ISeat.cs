using UnityEngine;

namespace _001_Scripts.Object.Vehicle
{
    public interface ISeat
    {
        bool IsOccupied { get; }
        Transform CameraAnchor { get; }
        IVehicleControllable Controller { get; }
        void Sit(Transform player);
        void Stand(Transform player, Transform standSpawnPoint, Transform reparentTo);
    }
}
