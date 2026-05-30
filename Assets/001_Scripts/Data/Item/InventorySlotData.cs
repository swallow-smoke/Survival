using _001_Scripts.Data.Item;

namespace _001_Scripts.Data.Item
{
    public readonly struct InventorySlotData
    {
        public readonly Item item;
        public readonly int count;

        public InventorySlotData(Item item, int count)
        {
            this.item = item;
            this.count = count;
        }
    }
}