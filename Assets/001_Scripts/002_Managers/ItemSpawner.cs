using System.Collections.Generic;
using System.Threading;
using _001_Scripts.Structure;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace _001_Scripts.Managers
{
    public class ItemSpawner : MonoBehaviour
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

        private IObjectResolver _resolver;
        private readonly List<GameObject> _alive = new();

        [Inject]
        public void Construct(IObjectResolver resolver)
        {
            _resolver = resolver;
        }

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
            var go = Instantiate(pickupPrefab, position, rotation);
            _resolver.InjectGameObject(go);

            var pickup = go.GetComponent<PickupObject>();
            pickup.Setup(itemId, count, displayName);

            return go;
        }
        
        public GameObject SpawnPickup(Vector3 position, int itemId, int count, string displayName = null)
            => SpawnPickup(position, Quaternion.identity, itemId, count, displayName);
    }
}