using _001_Scripts.Data.Message;
using _001_Scripts.Data.Structure.Interface;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace _001_Scripts.Structure
{
    public class PickupObject : MonoBehaviour, IInteractable, IInteractableInfo
    {
        [SerializeField] private int itemId;
        [SerializeField] private int count = 1;
        [SerializeField] private string displayName;

        private IPublisher<InvReqMessage> _invPublisher;

        [Inject]
        public void Construct(IPublisher<InvReqMessage> invPublisher)
        {
            _invPublisher = invPublisher;
        }

        public void Interact()
        {
            _invPublisher.Publish(new InvReqMessage(InvMessageType.Added, itemId, count));
            Destroy(gameObject);
        }

        public void Setup(int itemId, int count, string displayName = null)
        {
            this.itemId = itemId;
            this.count = count;
            if (displayName != null) this.displayName = displayName;
        }

        public string GetLabel() => $"Pick up {displayName}";
    }
}
