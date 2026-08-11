using _001_Scripts.Data.Message;
using _001_Scripts.Data.Structure.Interface;
using _001_Scripts.Entities;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace _001_Scripts.Structure
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(WorldItem))]
    public sealed class Pickup : EntityFeature, IInteractionTarget
    {
        private IPublisher<InvReqMessage> _inventoryPublisher;
        private WorldItem _item;

        protected override void Awake()
        {
            base.Awake();
            _item = GetComponent<WorldItem>();
            if (!_item) _item = gameObject.AddComponent<WorldItem>();
        }

        [Inject]
        public void Construct(IPublisher<InvReqMessage> inventoryPublisher)
            => _inventoryPublisher = inventoryPublisher;

        public void Interact()
        {
            if (_inventoryPublisher == null) return;
            _inventoryPublisher.Publish(new InvReqMessage(InvMessageType.Added, _item.ItemId, _item.Count));
            Destroy(Owner.gameObject);
        }

        public string GetLabel() => $"Pick up {Owner.DisplayName}";
    }
}
