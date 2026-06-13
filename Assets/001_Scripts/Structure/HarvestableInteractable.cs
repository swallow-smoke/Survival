using _001_Scripts.Data.Message;
using _001_Scripts.Data.Structure.Interface;
using _001_Scripts.Interface;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace _001_Scripts.Structure
{
    public class HarvestableInteractable : MonoBehaviour, IConditionalInteractable, IInteractableInfo, IDestructable
    {
        [SerializeField] private int requiredToolId;
        [SerializeField] private int dropItemId;
        [SerializeField] private int dropCount;
        [SerializeField] private float maxHP;
        [SerializeField] private string displayName;

        private float currentHP;
        private IInventoryService _invService;
        private IPublisher<InvReqMessage> _invPublisher;

        [Inject]
        public void Construct(IInventoryService invService, IPublisher<InvReqMessage> invPublisher)
        {
            _invService = invService;
            _invPublisher = invPublisher;
        }

        private void Awake()
        {
            currentHP = maxHP;
        }

        public bool CanInteract() => _invService.HasItem(requiredToolId);

        public string RequirementLabel() => "Tool required";

        public void Interact()
        {
            // TODO: tool damage value from ItemDataBase
            currentHP -= 10f;
            if (currentHP <= 0)
            {
                _invPublisher.Publish(new InvReqMessage(InvMessageType.Added, dropItemId, dropCount));
                Destroy();
            }
        }

        public void Destroy() => UnityEngine.Object.Destroy(gameObject);

        public string GetLabel() => $"Harvest {displayName}";
    }
}
