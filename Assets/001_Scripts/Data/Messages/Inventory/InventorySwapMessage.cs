namespace AstraNope.Data.Messages
{
    public enum InventorySlotArea
    {
        Inventory,
        Hotbar,
        Equipment
    }

    public readonly struct InventorySwapMessage
    {
        public readonly int fromIndex;
        public readonly int toIndex;
        public readonly InventorySlotArea fromArea;
        public readonly InventorySlotArea toArea;

        public InventorySwapMessage(int from, int to,
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
