using _001_Scripts.Data.Item;
using _001_Scripts.Interface;
using UnityEngine;
using WorldBuilder.Entities.Resources;

namespace _001_Scripts.Managers
{
    public sealed class InventoryHarvestToolSelector : IHarvestToolSelector
    {
        private readonly IInventoryService inventory;
        private readonly HarvestToolCatalog catalog;

        public InventoryHarvestToolSelector(IInventoryService inventory, HarvestToolCatalog catalog)
        {
            this.inventory = inventory;
            this.catalog = catalog;
        }

        public bool TrySelect(ResourceInteractionInfo resource, out HarvestToolSelection selection)
        {
            selection = default;
            if (!resource.IsResource || inventory == null || catalog == null) return false;

            if (resource.RequiredToolItemId < 0 &&
                (resource.AllowedMethods & HarvestMethod.Hand) != 0 &&
                catalog.HandPower >= resource.MinimumToolPower && resource.MinimumToolTier == 0)
            {
                selection = new HarvestToolSelection(HarvestMethod.Hand, -1, 0,
                    catalog.HandPower, catalog.HandDamage);
            }

            for (int i = 0; i < catalog.ToolCount; i++)
            {
                HarvestToolDefinition tool = catalog.GetTool(i);
                if (!inventory.HasItem(tool.itemId)) continue;
                if (resource.RequiredToolItemId >= 0 && tool.itemId != resource.RequiredToolItemId) continue;
                if ((resource.AllowedMethods & tool.method) == 0) continue;
                int tier = Mathf.Clamp(tool.tier, 0, byte.MaxValue);
                if (tier < resource.MinimumToolTier || tool.power < resource.MinimumToolPower) continue;
                if (selection.Method != HarvestMethod.None && tool.power <= selection.Power) continue;
                selection = new HarvestToolSelection(tool.method, tool.itemId, (byte)tier,
                    Mathf.Max(0f, tool.power), Mathf.Max(0f, tool.damage));
            }

            return selection.Method != HarvestMethod.None;
        }
    }
}
