using _001_Scripts.Entities;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace _001_Scripts.Managers
{
    public abstract class EntitySpawnerBase : MonoBehaviour
    {
        private IObjectResolver _resolver;

        [Inject]
        public void Construct(IObjectResolver resolver) => _resolver = resolver;

        protected GameObject SpawnPrefab(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (!prefab)
            {
                Debug.LogError($"[{GetType().Name}] Spawn prefab is not assigned.", this);
                return null;
            }

            var instance = Instantiate(prefab, position, rotation);
            _resolver?.InjectGameObject(instance);
            if (!instance.TryGetComponent<Entity>(out _))
                Debug.LogWarning($"[{GetType().Name}] Spawned object '{instance.name}' has no Entity.", instance);
            return instance;
        }
    }
}
