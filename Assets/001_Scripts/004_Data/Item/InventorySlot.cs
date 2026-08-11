using System;

namespace _001_Scripts.Data.Item
{
    [Serializable]
    public class InventorySlot
    {
        public Instance ins;
        public int stack;
        public bool IsEmpty => ins == null || stack <= 0;
        
        public InventorySlot(Instance ins, int stack) 
        {
            this.ins = ins;
            this.stack = stack;
        }
    }
}
