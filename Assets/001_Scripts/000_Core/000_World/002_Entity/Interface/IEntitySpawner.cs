using UnityEngine;

namespace _001_Scripts._000_Core._000_World._002_Entity.Interface
{
    public interface IPickupSpawner
    {
        GameObject SpawnPickup(Vector3 position, Quaternion rotation, int itemId, int count,
            string displayName = null);
        GameObject SpawnPickup(Vector3 position, int itemId, int count, string displayName = null);
    }

    public interface IVehicleSpawner
    {
        GameObject SpawnSmallSub(Vector3 position, Quaternion rotation);
        GameObject SpawnLargeSub(Vector3 position, Quaternion rotation);
    }
}
