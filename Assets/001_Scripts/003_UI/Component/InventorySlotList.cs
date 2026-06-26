using System.Collections.Generic;
using _001_Scripts.Data.Message;
using _001_Scripts.Data.SOJ;
using _001_Scripts.Interface;
using MessagePipe;
using UnityEngine;

namespace _001_Scripts.UI.Component
{
    public class InventorySlotList
    {
        private readonly List<ItemSlot> _slots = new();
        private readonly IInventoryService _inv;
        private readonly ItemDataBase _itemDB;

        public InventorySlotList(int count, GameObject slotPrefab, Transform parent,
            IPublisher<InvSwapMessage> swapPublisher, IInventoryService inv, ItemDataBase itemDB)
        {
            _inv = inv;
            _itemDB = itemDB;

            for (int i = 0; i < count; i++)
            {
                var go = UnityEngine.Object.Instantiate(slotPrefab, parent);
                var slot = go.GetComponent<ItemSlot>();
                slot.Init(swapPublisher);
                _slots.Add(slot);
            }
        }

        public void RefreshAll()
        {
            foreach (var slot in _slots)
                slot.Clear();

            var items = _inv.GetAllItems();
            for (int i = 0; i < items.Count && i < _slots.Count; i++)
                _slots[i].Set(_inv.GetSlot(i), _itemDB.GetItem(_inv.GetSlot(i).ins.itemId), i);
        }

        public void RefreshKeys(List<int> changedKeys)
        {
            changedKeys.ForEach(key =>
            {
                if (key >= _slots.Count)
                    return;
                var slot = _inv.GetSlot(key);
                if (slot.IsEmpty)
                    _slots[key].Clear();
                else
                    _slots[key].Set(slot, _itemDB.GetItem(slot.ins.itemId), key);
            });
        }
    }
}
