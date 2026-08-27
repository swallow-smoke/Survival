using System;
using System.Collections.Generic;
using System.Linq;
using AstraNope.Data.Items;
using AstraNope.Data.Messages;
using AstraNope.Data.Databases;
using AstraNope.Contracts;
using UnityEngine;

namespace AstraNope.Gameplay.Player
{
    public partial class InventoryController
    {
        private void SwapItem(InventorySwapMessage message)
        {
            NormalizeSlots();
            if (!IsValidAreaIndex(message.fromArea, message.fromIndex) ||
                !IsValidAreaIndex(message.toArea, message.toIndex)) return;

            var from = GetArea(message.fromArea);
            var to = GetArea(message.toArea);
            if (message.toArea == InventorySlotArea.Hotbar &&
                !CanPlaceInHotbar(from[message.fromIndex])) return;
            if (message.toArea == InventorySlotArea.Equipment &&
                !CanPlaceInEquipment(from[message.fromIndex], message.toIndex)) return;
            if (message.fromArea == InventorySlotArea.Equipment &&
                !CanPlaceInEquipment(to[message.toIndex], message.fromIndex)) return;
            (from[message.fromIndex], to[message.toIndex]) =
                (to[message.toIndex], from[message.fromIndex]);

            var inventoryChanged = new List<int>();
            var hotbarChanged = new List<int>();
            var equipmentChanged = new List<int>();
            AddChanged(message.fromArea, message.fromIndex, inventoryChanged, hotbarChanged, equipmentChanged);
            AddChanged(message.toArea, message.toIndex, inventoryChanged, hotbarChanged, equipmentChanged);
            PublishChanges(inventoryChanged, hotbarChanged, equipmentChanged);
        }

        private List<InventorySlot> GetArea(InventorySlotArea area) => area switch
        {
            InventorySlotArea.Hotbar => hotbarItems,
            InventorySlotArea.Equipment => equipmentItems,
            _ => items
        };

        private bool IsValidAreaIndex(InventorySlotArea area, int index)
        {
            var slots = GetArea(area);
            return index >= 0 && index < slots.Count;
        }

        private static void AddChanged(InventorySlotArea area, int index,
            List<int> inventoryChanged, List<int> hotbarChanged, List<int> equipmentChanged)
        {
            var target = area switch
            {
                InventorySlotArea.Hotbar => hotbarChanged,
                InventorySlotArea.Equipment => equipmentChanged,
                _ => inventoryChanged
            };
            if (!target.Contains(index)) target.Add(index);
        }

        private bool CanPlaceInEquipment(InventorySlot slot, int equipmentIndex)
        {
            if (slot == null || slot.IsEmpty) return true;
            if (equipmentIndex < 0 || equipmentIndex >= EquipmentSlots || itemDB == null) return false;
            Item item = itemDB.GetItem(slot.ins.itemId);
            return item.TryGetFeature<IEquipmentItem>(out var equipment) &&
                   equipment.SlotType == GetEquipmentSlotType(equipmentIndex);
        }

        private bool CanPlaceInHotbar(InventorySlot slot)
        {
            if (slot == null || slot.IsEmpty) return true;
            if (itemDB == null) return true;
            return !itemDB.GetItem(slot.ins.itemId).HasFeature<IEquipmentItem>();
        }

        public EquipmentSlotType GetEquipmentSlotType(int index) => index switch
        {
            0 => EquipmentSlotType.Head,
            1 => EquipmentSlotType.Body,
            2 => EquipmentSlotType.Legs,
            3 => EquipmentSlotType.Feet,
            >= 4 and < EquipmentSlots => EquipmentSlotType.UpgradeChip,
            _ => EquipmentSlotType.None
        };

        public InventorySlot GetEquipmentSlot(int index)
        {
            NormalizeSlots();
            if (index < 0 || index >= EquipmentSlots)
                throw new IndexOutOfRangeException($"Equipment index {index} is out of range.");
            return equipmentItems[index];
        }

        public AddItemResult AddItem(int id, int count)
        {
            NormalizeSlots();
            if (count <= 0) return new AddItemResult(0, new List<int>());

            var template = itemDB.GetItem(id);
            int remaining = count;
            var changed = new List<int>();

            if (template.TryGetFeature<IStackable>(out var stackable))
            {
                int maximum = stackable.MaxStack;
                for (int i = 0; i < items.Count && remaining > 0; i++)
                {
                    var slot = items[i];
                    if (slot.IsEmpty || slot.ins.itemId != id || slot.stack >= maximum) continue;
                    int amount = Mathf.Min(remaining, maximum - slot.stack);
                    slot.stack += amount;
                    remaining -= amount;
                    changed.Add(i);
                }

                while (remaining > 0)
                {
                    int emptyIndex = FindEmptySlot();
                    if (emptyIndex < 0) break;
                    int amount = Mathf.Min(remaining, maximum);
                    items[emptyIndex] = new InventorySlot(itemDB.CreateInstance(id), amount);
                    remaining -= amount;
                    changed.Add(emptyIndex);
                }
            }
            else
            {
                while (remaining > 0)
                {
                    int emptyIndex = FindEmptySlot();
                    if (emptyIndex < 0) break;
                    items[emptyIndex] = new InventorySlot(itemDB.CreateInstance(id), 1);
                    remaining--;
                    changed.Add(emptyIndex);
                }
            }

            PublishChanges(changed);
            int added = count - remaining;
            if (added > 0) PublishItemNotification(id, added, NotificationKind.ItemAdded);
            return new AddItemResult(remaining, changed);
        }

        public void RemoveItem(int id, int count)
        {
            if (count <= 0) return;
            int remaining = count;
            var changed = new List<int>();
            var hotbarChanged = new List<int>();
            for (int i = items.Count - 1; i >= 0 && remaining > 0; i--)
            {
                var slot = items[i];
                if (slot.IsEmpty || slot.ins.itemId != id) continue;
                int amount = Mathf.Min(remaining, slot.stack);
                slot.stack -= amount;
                remaining -= amount;
                if (slot.stack <= 0) items[i] = EmptySlot();
                changed.Add(i);
            }
            for (int i = hotbarItems.Count - 1; i >= 0 && remaining > 0; i--)
            {
                var slot = hotbarItems[i];
                if (slot.IsEmpty || slot.ins.itemId != id) continue;
                int amount = Mathf.Min(remaining, slot.stack);
                slot.stack -= amount;
                remaining -= amount;
                if (slot.stack <= 0) hotbarItems[i] = EmptySlot();
                hotbarChanged.Add(i);
            }
            PublishChanges(changed, hotbarChanged);
            int removed = count - remaining;
            if (removed > 0) PublishItemNotification(id, removed, NotificationKind.ItemRemoved);
        }

        public void RemoveItem(Item item) => RemoveAll(slot => slot.ins.itemId == item.itemId);

        public void RemoveItem(ItemInstance instance) => RemoveAll(slot => slot.ins.instanceId == instance.instanceId);

        public bool HasItem(int id, int count = 1) =>
            items.Concat(hotbarItems).Where(slot => slot != null && !slot.IsEmpty && slot.ins.itemId == id)
                .Sum(slot => slot.stack) >= count;

        public bool HasItem(Item item, int count = 1) => HasItem(item.itemId, count);

        public bool HasItem(ItemInstance instance) =>
            items.Concat(hotbarItems)
                .Any(slot => slot != null && !slot.IsEmpty && slot.ins.instanceId == instance.instanceId);

        public IReadOnlyList<InventorySlot> GetAllItems()
        {
            NormalizeSlots();
            return items.AsReadOnly();
        }

        public InventorySlot GetSlot(int index)
        {
            NormalizeSlots();
            if (!IsValidIndex(index))
                throw new IndexOutOfRangeException($"Index {index} is out of range for inventory slots.");
            return items[index];
        }

        public InventorySlot GetHotbarSlot(int index)
        {
            if (index < 0 || index >= HotbarSlotCount)
                throw new IndexOutOfRangeException($"Hotbar index {index} is out of range.");
            return hotbarItems[index];
        }
        public bool UseItem(int index)
        {
            if (!IsValidIndex(index) || items[index].IsEmpty) return false;
            var item = itemDB.GetItem(items[index].ins.itemId);
            if (!item.TryGetFeature<IUsable>(out var usable)) return false;

            var player = GetComponent<PlayerController>();
            if (player == null) return false;
            usable.Use(player);
            RemoveAt(index, 1);
            return true;
        }

        public bool DropItem(int index, int count = 1)
        {
            if (!IsValidIndex(index) || items[index].IsEmpty || count <= 0) return false;
            RemoveAt(index, count);
            return true;
        }

        public void SortItems()
        {
            var occupied = items.Where(slot => slot != null && !slot.IsEmpty)
                .OrderBy(slot => itemDB.GetItem(slot.ins.itemId).itemType)
                .ThenBy(slot => itemDB.GetItem(slot.ins.itemId).itemName)
                .ThenBy(slot => slot.ins.itemId)
                .ToList();

            for (int i = 0; i < items.Count; i++)
                items[i] = i < occupied.Count ? occupied[i] : EmptySlot();
            PublishChanges(Enumerable.Range(0, items.Count).ToList());
        }

        private void RemoveAt(int index, int count)
        {
            var slot = items[index];
            slot.stack -= Mathf.Min(count, slot.stack);
            if (slot.stack <= 0) items[index] = EmptySlot();
            PublishChanges(index);
        }

        private void RemoveAll(Func<InventorySlot, bool> predicate)
        {
            var changed = new List<int>();
            var hotbarChanged = new List<int>();
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].IsEmpty || !predicate(items[i])) continue;
                items[i] = EmptySlot();
                changed.Add(i);
            }
            for (int i = 0; i < hotbarItems.Count; i++)
            {
                if (hotbarItems[i].IsEmpty || !predicate(hotbarItems[i])) continue;
                hotbarItems[i] = EmptySlot();
                hotbarChanged.Add(i);
            }
            PublishChanges(changed, hotbarChanged);
        }

        private int FindEmptySlot() => items.FindIndex(slot => slot == null || slot.IsEmpty);
        private bool IsValidIndex(int index)
        {
            NormalizeSlots();
            return index >= 0 && index < items.Count;
        }

        private void PublishChanges(params int[] indices) => PublishChanges(indices.ToList());

        private void PublishChanges(List<int> indices, List<int> hotbarIndices = null,
            List<int> equipmentIndices = null)
        {
            hotbarIndices ??= new List<int>();
            equipmentIndices ??= new List<int>();
            if (hotbarIndices.Contains(_selectedHotbarIndex)) SyncHeldItem();
            if (indices.Count > 0 || hotbarIndices.Count > 0 || equipmentIndices.Count > 0)
                _invChangedPublisher?.Publish(new InventoryChangedMessage(indices, hotbarIndices, equipmentIndices));
        }
        private void OnMessageReceived(InventoryRequestMessage message)
        {
            switch (message.msgType)
            {
                case InvMessageType.Added:
                    var result = AddItem(message.item, message.count);
                    int added = message.count - result.remain;
                    Debug.Log($"[Inventory] {itemDB.GetItem(message.item).itemName} +{added}" +
                              (result.remain > 0 ? $" ({result.remain} did not fit)" : ""));
                    break;
                case InvMessageType.Removed:
                    RemoveItem(message.item, message.count);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}