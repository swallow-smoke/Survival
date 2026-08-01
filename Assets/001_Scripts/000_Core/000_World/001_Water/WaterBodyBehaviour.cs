using _001_Scripts.Core._000_World._001_Water.Interface;
using UnityEngine;
using VContainer;

namespace _001_Scripts.Core._000_World._001_Water
{
    public abstract class WaterBodyBehaviour : MonoBehaviour, IWaterBody
    {
        [SerializeField] private int priority;
        [SerializeField] private bool showGizmos = true;

        private IWaterRegistry _registry;
        private bool _registered;

        public int Priority => priority;
        public bool ShowGizmos => showGizmos;
        public abstract Bounds WorldBounds { get; }
        public abstract bool TrySample(Vector3 worldPosition, out WaterSample sample);

        protected virtual void OnEnable()
        {
            WaterRegistryLocator.RegistryAvailable += OnRegistryAvailable;
            if (_registry == null) _registry = WaterRegistryLocator.Current;
            TryRegister();
        }

        protected virtual void OnDisable()
        {
            WaterRegistryLocator.RegistryAvailable -= OnRegistryAvailable;
            TryUnregister();
        }
        protected virtual void OnDestroy() => TryUnregister();

        [Inject]
        public void ConstructWaterRegistry(IWaterRegistry registry)
        {
            if (ReferenceEquals(_registry, registry))
            {
                TryRegister();
                return;
            }

            TryUnregister();
            _registry = registry;
            TryRegister();
        }

        private void TryRegister()
        {
            if (_registered || !isActiveAndEnabled || _registry == null) return;
            _registered = _registry.Register(this);
        }

        private void OnRegistryAvailable(IWaterRegistry registry) => ConstructWaterRegistry(registry);

        protected void NotifyBoundsChanged()
        {
            if (_registered && _registry != null) _registry.Refresh(this);
        }

        private void TryUnregister()
        {
            if (!_registered) return;
            if (_registry != null) _registry.Unregister(this);
            _registered = false;
        }
    }
}
