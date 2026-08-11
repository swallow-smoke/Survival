using System.Collections.Generic;
using System.Threading;
using _001_Scripts.Entities;
using _001_Scripts.Interface;
using _001_Scripts.Structure;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _001_Scripts.Managers
{
    public class ItemSpawner : EntitySpawnerBase, IPickupSpawner
    {
        [SerializeField] private GameObject pickupPrefab;
        [SerializeField] private Transform playerTrs;

        [Header("Spawn Settings")]
        [SerializeField] private float spawnInterval = 10f;
        [SerializeField] private float spawnRadius = 8f;
        [SerializeField] private int maxAlive = 5;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float groundCheckHeight = 20f;

        [Header("Spawn Item")]
        [SerializeField] private int itemId;
        [SerializeField] private int count = 1;
        [SerializeField] private string displayName;

        private readonly List<GameObject> _alive = new();

        private void Start()
        {
            SpawnLoop(this.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTaskVoid SpawnLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                await UniTask.Delay(System.TimeSpan.FromSeconds(spawnInterval), cancellationToken: ct);

                _alive.RemoveAll(go => go == null);

                if (_alive.Count >= maxAlive)
                    continue;

                if (TryGetSpawnPosition(out Vector3 pos, out Quaternion rot))
                {
                    var go = SpawnPickup(pos, rot, itemId, count, displayName);
                    _alive.Add(go);
                }
            }
        }

        private bool TryGetSpawnPosition(out Vector3 position, out Quaternion rotation)
        {
            Vector2 offset = Random.insideUnitCircle * spawnRadius;
            Vector3 candidate = playerTrs.position + new Vector3(offset.x, 0, offset.y);

            Vector3 rayOrigin = candidate + Vector3.up * groundCheckHeight;

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, groundCheckHeight * 2f, groundLayer))
            {
                position = hit.point;
                rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                return true;
            }

            position = default;
            rotation = Quaternion.identity;
            return false;
        }

        public GameObject SpawnPickup(Vector3 position, Quaternion rotation, int itemId, int count, string displayName = null)
        {
            var go = SpawnPrefab(pickupPrefab, position, rotation);
            if (!go) return null;

            var pickup = go.GetComponent<Pickup>();
            if (!pickup)
            {
                Debug.LogError("[ItemSpawner] Pickup prefab requires Pickup.", go);
                Destroy(go);
                return null;
            }
            go.GetComponent<WorldItem>().Setup(itemId, count, displayName);

            return go;
        }
        
        public GameObject SpawnPickup(Vector3 position, int itemId, int count, string displayName = null)
            => SpawnPickup(position, Quaternion.identity, itemId, count, displayName);
    }
}
