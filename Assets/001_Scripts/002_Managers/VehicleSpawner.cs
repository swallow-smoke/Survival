using _001_Scripts.Interface;
using UnityEngine;

namespace _001_Scripts.Managers
{
    public class VehicleSpawner : EntitySpawnerBase, IVehicleSpawner
    {
        [SerializeField] private GameObject smallSubPrefab;
        [SerializeField] private GameObject largeSubPrefab;

        [SerializeField] private Transform _trs;
        [SerializeField] private Quaternion _rot;

        public GameObject SpawnSmallSub(Vector3 position, Quaternion rotation)
            => SpawnPrefab(smallSubPrefab, position, rotation);

        public void SpawnSmallSub() => SpawnSmallSub(_trs.position, _rot);

        public GameObject SpawnLargeSub(Vector3 position, Quaternion rotation)
            => SpawnPrefab(largeSubPrefab, position, rotation);
    }
}
