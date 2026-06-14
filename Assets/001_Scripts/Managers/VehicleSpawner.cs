using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace _001_Scripts.Managers
{
    public class VehicleSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject smallSubPrefab;
        [SerializeField] private GameObject largeSubPrefab;

        [SerializeField] private Transform _trs;
        [SerializeField] private Quaternion _rot;

        private IObjectResolver _resolver;

        [Inject]
        public void Construct(IObjectResolver resolver) => _resolver = resolver;

        public GameObject SpawnSmallSub(Vector3 position, Quaternion rotation)
        {
            var go = Instantiate(smallSubPrefab, position, rotation);
            _resolver.InjectGameObject(go);
            return go;
        }

        public void SpawnSmallSub() => SpawnSmallSub(_trs.position, _rot);

        public GameObject SpawnLargeSub(Vector3 position, Quaternion rotation)
        {
            var go = Instantiate(largeSubPrefab, position, rotation);
            _resolver.InjectGameObject(go);
            return go;
        }
    }
}
