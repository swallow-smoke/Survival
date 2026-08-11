using UnityEngine;

namespace _001_Scripts.Entities
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Vehicle))]
    public sealed class Submarine : EntityFeature
    {
        [SerializeField] private bool supportsInterior;
        public bool SupportsInterior => supportsInterior;

        protected override void Awake()
        {
            base.Awake();
            if (!GetComponent<Vehicle>()) gameObject.AddComponent<Vehicle>();
            Owner.SetKind(EntityKind.Submarine);
        }
    }
}
