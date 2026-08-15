namespace _001_Scripts.Data.Message
{
    public enum InventorySlotArea
    {
        Inventory,
        Hotbar,
        Equipment
    }

    public readonly struct InvSwapMessage
    {
        public readonly int fromIndex;
        public readonly int toIndex;
        public readonly InventorySlotArea fromArea;
        public readonly InventorySlotArea toArea;

        public InvSwapMessage(int from, int to,
            InventorySlotArea fromArea = InventorySlotArea.Inventory,
            InventorySlotArea toArea = InventorySlotArea.Inventory)
        {
            fromIndex = from;
            toIndex = to;
            this.fromArea = fromArea;
            this.toArea = toArea;
        }
    }
}
