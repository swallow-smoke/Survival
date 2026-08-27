using UnityEngine;
using AstraNope.WorldObjects.Entities;
using VContainer;
using VContainer.Unity;

namespace AstraNope.WorldObjects.Vehicles.Core
{
    public class VehicleInjector : MonoBehaviour
    {
        private IObjectResolver _resolver;

        [Inject]
        public void Construct(IObjectResolver resolver) => _resolver = resolver;

        private void Start()
        {
            foreach (var vehicle in FindObjectsByType<Submarine>(FindObjectsSortMode.None))
                _resolver.InjectGameObject(vehicle.gameObject);
        }
    }
}
