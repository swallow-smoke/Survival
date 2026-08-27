using System.Collections.Generic;
using AstraNope.Data.Messages;
using AstraNope.Data.Databases;
using AstraNope.Contracts;
using MessagePipe;

namespace AstraNope.UI.Components
{
    public class InventorySlotList
    {
        private readonly List<ItemSlot> _slots;
        private readonly IInventoryService _inventory;
        private readonly ItemDataBase _itemDatabase;

        public InventorySlotList(List<ItemSlot> slots, IPublisher<InventorySwapMessage> swapPublisher,
            IInventoryService inventory, ItemDataBase itemDatabase)
        {
            _slots = slots;
            _inventory = inventory;
            _itemDatabase = itemDatabase;
            for (int i = 0; i < _slots.Count; i++)
                _slots[i].Init(swapPublisher, i, InventorySlotArea.Inventory);
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
