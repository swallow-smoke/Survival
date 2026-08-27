using AstraNope.WorldObjects.Entities;
using UnityEngine;
using VContainer;
using VContainer.Unity;

using AstraNope.Localization;
namespace AstraNope.WorldObjects.Structures
{
    public sealed class SubmarineFabricator : Fabricator
    {
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private GameObject submarinePrefab;
        [SerializeField] private float fabricateCooldown = 1f;

        private GameObject _lastPrototype;
        private float _lastFabricatedAt = -10f;
        private IObjectResolver _resolver;

        [Inject]
        private void Construct(IObjectResolver resolver) => _resolver = resolver;

        protected override void Awake()
        {
            base.Awake();
            Configure("SubmarineFabricator", L10n.T("k_e6e7280ac6"));
        }

        public void Configure(Transform point, GameObject prefab = null)
        {
            spawnPoint = point;
            if (prefab) submarinePrefab = prefab;
        }

        public bool TryFabricatePrototype(out string message)
        {
            if (Time.unscaledTime - _lastFabricatedAt < fabricateCooldown)
            {
                message = L10n.T("k_9900922556");
                return false;
            }

            if (_lastPrototype)
            {
                message = L10n.T("k_fc4e0d6a47");
                return false;
            }

            if (!submarinePrefab)
            {
                message = L10n.T("k_22d62a367e");
                return false;
            }

            Transform point = spawnPoint ? spawnPoint : transform;
            _lastPrototype = Instantiate(submarinePrefab, point.position, point.rotation);
            _lastPrototype.name = "FabricatedSmallSubmarine";
            _resolver?.InjectGameObject(_lastPrototype);
            _lastFabricatedAt = Time.unscaledTime;
            message = L10n.T("k_dfb182ea8c");
            return true;
        }
    }

}
