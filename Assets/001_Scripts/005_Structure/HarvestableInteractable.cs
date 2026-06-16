using _001_Scripts.Data.Structure.Interface;
using _001_Scripts.Interface;
using _001_Scripts.Managers;
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
        private ItemSpawner _itemSpawner;

        [Inject]
        public void Construct(IInventoryService invService, ItemSpawner itemSpawner)
        {
            _invService = invService;
            _itemSpawner = itemSpawner;
        }

        private void Awake() => currentHP = maxHP;

        public bool CanInteract() => _invService.HasItem(requiredToolId);
        public string RequirementLabel() => "Tool required";

        public void Interact()
        {
            currentHP -= 10f;
            if (currentHP <= 0)
            {
                _itemSpawner.SpawnPickup(transform.position, dropItemId, dropCount);
                Destroy();
            }
        }

        public void Destroy() => UnityEngine.Object.Destroy(gameObject);
        public string GetLabel() => $"Harvest {displayName}";
    }
}
