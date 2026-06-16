using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace _001_Scripts.Structure
{
    public class VehicleInjector : MonoBehaviour
    {
        private IObjectResolver _resolver;

        [Inject]
        public void Construct(IObjectResolver resolver) => _resolver = resolver;

        private void Start()
        {
            foreach (var vehicle in FindObjectsByType<VehicleBody>(FindObjectsSortMode.None))
                _resolver.InjectGameObject(vehicle.gameObject);
        }
    }
}
