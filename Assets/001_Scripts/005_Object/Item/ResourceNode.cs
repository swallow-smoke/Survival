using _001_Scripts.Data.Structure.Interface;
using _001_Scripts.Entities;
using _001_Scripts.Interface;
using UnityEngine;
using VContainer;

namespace _001_Scripts.Structure
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(WorldItem), typeof(Health))]
    public sealed class ResourceNode : EntityFeature, IConditionalInteractionTarget, IDestroyable
    {
        [SerializeField] private int requiredToolId;
        [SerializeField, Min(.01f)] private float damagePerInteraction = 10f;

        private IInventoryReader _inventory;
        private IPickupSpawner _pickupSpawner;
        private WorldItem _item;
        private Health _health;
        private bool _dropSpawned;

        protected override void Awake()
        {
            base.Awake();
            Owner.SetKind(EntityKind.ResourceNode);
            _item = GetComponent<WorldItem>();
            if (!_item) _item = gameObject.AddComponent<WorldItem>();
            _health = GetComponent<Health>();
            if (!_health) _health = gameObject.AddComponent<Health>();
            _health.Died += SpawnDrop;
        }

        protected override void OnDisable()
        {
            if (_health) _health.Died -= SpawnDrop;
            base.OnDisable();
        }

        [Inject]
        public void Construct(IInventoryReader inventory, IPickupSpawner pickupSpawner)
        {
            _inventory = inventory;
            _pickupSpawner = pickupSpawner;
        }

        public bool CanInteract() => requiredToolId <= 0 || _inventory != null && _inventory.HasItem(requiredToolId);
        public string RequirementLabel() => "Tool required";
        public string GetLabel() => $"Harvest {Owner.DisplayName}";

        public void Interact()
        {
            if (CanInteract()) _health.ApplyDamage(damagePerInteraction);
        }

        public void Destroy() => _health.ApplyDamage(_health.CurrentHealth);

        private void SpawnDrop()
        {
            if (_dropSpawned || _pickupSpawner == null) return;
            _dropSpawned = true;
            _pickupSpawner.SpawnPickup(transform.position, _item.ItemId, _item.Count, Owner.DisplayName);
        }
    }
}
