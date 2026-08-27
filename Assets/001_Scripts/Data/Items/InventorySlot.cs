using System;

namespace AstraNope.Data.Items
{
    [Serializable]
    public class InventorySlot
    {
        public ItemInstance ins;
        public int stack;
        public bool IsEmpty => ins == null || stack <= 0;
        
        public InventorySlot(ItemInstance ins, int stack) 
        {
            this.ins = ins;
            this.stack = stack;
        }
    }
}
