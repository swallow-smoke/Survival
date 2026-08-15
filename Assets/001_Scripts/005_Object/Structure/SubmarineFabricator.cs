using _001_Scripts.Entities;
using _001_Scripts.UI;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace _001_Scripts.Structure
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
            Configure("SubmarineFabricator", "잠수함 제작대 사용");
        }

        public void Configure(Transform point, GameObject prefab = null)
        {
            spawnPoint = point;
            if (prefab) submarinePrefab = prefab;
        }

        protected override void BeforePanelOpen()
        {
            var panel = FindAnyObjectByType<SubmarineFabricatorPanel>(FindObjectsInactive.Include);
            if (panel) panel.SetStation(this);
        }

        public bool TryFabricatePrototype(out string message)
        {
            if (Time.unscaledTime - _lastFabricatedAt < fabricateCooldown)
            {
                message = "제작 장치 냉각 중";
                return false;
            }

            if (_lastPrototype)
            {
                message = "임시 잠수함이 이미 배치되어 있습니다";
                return false;
            }

            if (!submarinePrefab)
            {
                message = "잠수함 프리팹이 지정되지 않았습니다.";
                return false;
            }

            Transform point = spawnPoint ? spawnPoint : transform;
            _lastPrototype = Instantiate(submarinePrefab, point.position, point.rotation);
            _lastPrototype.name = "FabricatedSmallSubmarine";
            _resolver?.InjectGameObject(_lastPrototype);
            _lastFabricatedAt = Time.unscaledTime;
            message = "임시 잠수함 제작 완료";
            return true;
        }
    }

}
