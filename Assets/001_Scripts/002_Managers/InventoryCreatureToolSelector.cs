using _001_Scripts.Data.Item;
using _001_Scripts.Interface;
using UnityEngine;

namespace _001_Scripts.Managers
{
    public sealed class InventoryCreatureToolSelector : ICreatureToolSelector
    {
        private readonly IHotbarReader hotbar;
        private readonly HarvestToolCatalog catalog;

        public InventoryCreatureToolSelector(IHotbarReader hotbar, HarvestToolCatalog catalog)
        {
            this.hotbar = hotbar;
            this.catalog = catalog;
        }

        public CreatureToolSelection Select()
        {
            if (hotbar == null || hotbar.HotbarSlotCount <= 0) return new CreatureToolSelection(-1, 0);
            InventorySlot heldSlot = hotbar.GetHotbarSlot(hotbar.SelectedHotbarIndex);
            if (heldSlot == null || heldSlot.IsEmpty) return new CreatureToolSelection(-1, 0);

            int heldItemId = heldSlot.ins.itemId;
            byte tier = 0;
            if (catalog != null)
            {
                for (int i = 0; i < catalog.ToolCount; i++)
                {
                    HarvestToolDefinition tool = catalog.GetTool(i);
                    if (tool.itemId != heldItemId) continue;
                    tier = (byte)Mathf.Clamp(tool.tier, 0, byte.MaxValue);
                    break;
                }
            }

            return new CreatureToolSelection(heldItemId, tier);
        }
    }
}
