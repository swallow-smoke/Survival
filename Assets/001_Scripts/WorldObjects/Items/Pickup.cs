using AstraNope.Data.Messages;
using AstraNope.Contracts.WorldObjects;
using AstraNope.WorldObjects.Entities;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace AstraNope.WorldObjects.Items
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(WorldItem))]
    public sealed class Pickup : EntityFeature, IInteractionTarget
    {
        private IPublisher<InventoryRequestMessage> _inventoryPublisher;
        private WorldItem _item;

        protected override void Awake()
        {
            base.Awake();
            _item = GetComponent<WorldItem>();
            if (!_item) _item = gameObject.AddComponent<WorldItem>();
        }

        [Inject]
        public void Construct(IPublisher<InventoryRequestMessage> inventoryPublisher)
            => _inventoryPublisher = inventoryPublisher;

        public void Interact()
        {
            if (_inventoryPublisher == null) return;
            _inventoryPublisher.Publish(new InventoryRequestMessage(InvMessageType.Added, _item.ItemId, _item.Count));
            Destroy(Owner.gameObject);
        }

        public string GetLabel() => $"Pick up {Owner.DisplayName}";
    }
}
