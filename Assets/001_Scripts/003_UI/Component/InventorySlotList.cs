using System.Collections.Generic;
using _001_Scripts.Data.Message;
using _001_Scripts.Data.SOJ;
using _001_Scripts.Interface;
using MessagePipe;

namespace _001_Scripts.UI.Component
{
    public class InventorySlotList
    {
        private readonly List<ItemSlot> _slots;
        private readonly IInventoryService _inventory;
        private readonly ItemDataBase _itemDatabase;

        public InventorySlotList(List<ItemSlot> slots, IPublisher<InvSwapMessage> swapPublisher,
            IInventoryService inventory, ItemDataBase itemDatabase)
        {
            _slots = slots;
            _inventory = inventory;
            _itemDatabase = itemDatabase;
            for (int i = 0; i < _slots.Count; i++)
                _slots[i].Init(swapPublisher, i);
        }

        public void RefreshAll()
        {
            for (int i = 0; i < _slots.Count; i++)
                Refresh(i);
        }

        public void RefreshKeys(List<int> changedKeys)
        {
            foreach (int key in changedKeys)
                if (key >= 0 && key < _slots.Count)
                    Refresh(key);
        }

        public void SetSelected(int index)
        {
            for (int i = 0; i < _slots.Count; i++)
                _slots[i].SetSelected(i == index);
        }

        private void Refresh(int index)
        {
            var items = _inventory.GetAllItems();
            if (index < 0 || index >= items.Count)
            {
                _slots[index].Clear();
                return;
            }

            var slot = items[index];
            if (slot == null || slot.IsEmpty)
                _slots[index].Clear();
            else
                _slots[index].Set(slot, _itemDatabase.GetItem(slot.ins.itemId), index);
        }
    }
}
