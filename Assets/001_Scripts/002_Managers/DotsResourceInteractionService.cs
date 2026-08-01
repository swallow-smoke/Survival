using _001_Scripts.Data.Item;
using _001_Scripts.Interface;
using Unity.Entities;
using UnityEngine;
using WorldBuilder.Entities.Resources;

namespace _001_Scripts.Managers
{
    public sealed class DotsResourceInteractionService : IResourceInteractionService
    {
        private readonly IWorldResourceGateway gateway;
        private readonly IHarvestToolSelector toolSelector;
        private Entity focusedEntity = Entity.Null;
        private bool focusedIsDroppedItem;
        private bool canInteract;
        private HarvestToolSelection selectedTool;

        public DotsResourceInteractionService(IWorldResourceGateway gateway, IHarvestToolSelector toolSelector)
        {
            this.gateway = gateway;
            this.toolSelector = toolSelector;
        }

        public bool TryFocus(Vector3 origin, Vector3 direction, float distance, out ResourceInteractionFocus focus)
        {
            focus = default;
            if (!gateway.TryRaycast(origin, direction, distance, out Entity target) ||
                !gateway.TryGetInteractionInfo(target, out ResourceInteractionInfo info))
            {
                ClearFocus();
                return false;
            }

            focusedEntity = target;
            focusedIsDroppedItem = info.IsDroppedItem;
            selectedTool = default;
            canInteract = info.IsDroppedItem || toolSelector.TrySelect(info, out selectedTool);
            string label = info.IsDroppedItem
                ? $"Pick up {info.DisplayName}"
                : canInteract ? $"Harvest {info.DisplayName}" : "Required harvesting tool";
            focus = new ResourceInteractionFocus(canInteract, label);
            return true;
        }

        public bool InteractFocused()
        {
            if (focusedEntity == Entity.Null || !canInteract) return false;
            return focusedIsDroppedItem
                ? gateway.TryPickup(focusedEntity)
                : gateway.TryHarvest(focusedEntity, selectedTool);
        }

        public void ClearFocus()
        {
            focusedEntity = Entity.Null;
            focusedIsDroppedItem = false;
            canInteract = false;
            selectedTool = default;
        }
    }
}
