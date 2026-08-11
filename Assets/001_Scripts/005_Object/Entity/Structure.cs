using _001_Scripts.Data.Structure.Interface;
using UnityEngine;

namespace _001_Scripts.Entities
{
    [DisallowMultipleComponent]
    public sealed class Structure : EntityFeature, IPlaceable
    {
        [SerializeField] private bool isPlaced = true;
        [SerializeField] private bool canRotate = true;

        public bool IsPlaced => isPlaced;
        public bool CanRotate => canRotate;

        protected override void Awake()
        {
            base.Awake();
            Owner.SetKind(EntityKind.Structure);
        }

        public void Place() => isPlaced = true;
        public void Remove() => isPlaced = false;
    }
}
