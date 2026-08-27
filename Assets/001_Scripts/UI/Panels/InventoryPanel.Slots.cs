using System;
using AstraNope.UI.Components;
using AstraNope.Data.Items;
using AstraNope.Data.Messages;
using AstraNope.Data.Databases;
using AstraNope.Contracts;
using UnityEngine;

using AstraNope.Localization;
namespace AstraNope.UI.Panels
{
    public partial class InventoryPanel
    {
        private void OnSlotSelected(int index)
        {
            if (TryEquipFromInventory(index))
            {
                _selectedIndex = -1;
                _slotList?.SetSelected(-1);
                return;
            }
            _selectedIndex = index;
            _selectedArea = InventorySlotArea.Inventory;
            _slotList?.SetSelected(index);
            SetEquipmentSelected(-1);
            ShowDetails(_selectedArea, index);
        }

        private void OnEquipmentSlotSelected(int index)
        {
            if (_equipment == null || _invSwapPublisher == null || index < 0 ||
                index >= _equipment.EquipmentSlotCount) return;
            InventorySlot equipped = _equipment.GetEquipmentSlot(index);
            if (equipped == null || equipped.IsEmpty) return;
            int emptyIndex = FindFirstEmptyInventorySlot();
            if (emptyIndex < 0) return;
            _invSwapPublisher.Publish(new InventorySwapMessage(index, emptyIndex,
                InventorySlotArea.Equipment, InventorySlotArea.Inventory));
            _selectedIndex = -1;
            SetEquipmentSelected(-1);
        }

        private bool TryEquipFromInventory(int inventoryIndex)
        {
            if (_inventory == null || _equipment == null || _invSwapPublisher == null || itemDB == null ||
                inventoryIndex < 0 || inventoryIndex >= _inventory.SlotCount) return false;
            InventorySlot slot = _inventory.GetSlot(inventoryIndex);
            if (slot == null || slot.IsEmpty) return false;
            Item item = itemDB.GetItem(slot.ins.itemId);
            if (!item.TryGetFeature<IEquipmentItem>(out var equipmentItem)) return false;
            int targetIndex = -1;
            for (int i = 0; i < _equipment.EquipmentSlotCount; i++)
            {
                if (_equipment.GetEquipmentSlotType(i) != equipmentItem.SlotType) continue;
                if (targetIndex < 0) targetIndex = i;
                InventorySlot equipped = _equipment.GetEquipmentSlot(i);
                if (equipped == null || equipped.IsEmpty)
                {
                    targetIndex = i;
                    break;
                }
            }
            if (targetIndex < 0) return false;
            _invSwapPublisher.Publish(new InventorySwapMessage(inventoryIndex, targetIndex,
                InventorySlotArea.Inventory, InventorySlotArea.Equipment));
            return true;
        }
        private void AssignSelectedToHotbar(int hotbarIndex)
        {
            if (!isViz || _selectedArea != InventorySlotArea.Inventory ||
                _inventory == null || _hotbar == null || _invSwapPublisher == null ||
                hotbarIndex < 0 || hotbarIndex >= _hotbar.HotbarSlotCount) return;

            int inventoryIndex = _selectedIndex;
            var hotbarSlot = _hotbar.GetHotbarSlot(hotbarIndex);
            bool hotbarOccupied = hotbarSlot != null && !hotbarSlot.IsEmpty;

            if (inventoryIndex < 0 || inventoryIndex >= _inventory.SlotCount)
            {
                if (!hotbarOccupied) return;
                inventoryIndex = FindFirstEmptyInventorySlot();
                if (inventoryIndex < 0) return;
                _selectedIndex = inventoryIndex;
                _slotList?.SetSelected(inventoryIndex);
            }

            var inventorySlot = _inventory.GetSlot(inventoryIndex);
            bool inventoryOccupied = inventorySlot != null && !inventorySlot.IsEmpty;
            if (!inventoryOccupied && !hotbarOccupied) return;

            _invSwapPublisher.Publish(new InventorySwapMessage(inventoryIndex, hotbarIndex,
                InventorySlotArea.Inventory, InventorySlotArea.Hotbar));
        }

        private int FindFirstEmptyInventorySlot()
        {
            for (int i = 0; i < _inventory.SlotCount; i++)
            {
                var slot = _inventory.GetSlot(i);
                if (slot == null || slot.IsEmpty) return i;
            }

            return -1;
        }
        private void UseSelected()
        {
            if (_selectedArea != InventorySlotArea.Inventory || _selectedIndex < 0 ||
                _inventory == null || !_inventory.UseItem(_selectedIndex)) return;
            _slotList?.RefreshAll();
            ShowDetails(_selectedIndex);
        }

        private void DropSelected()
        {
            if (_selectedArea != InventorySlotArea.Inventory || _selectedIndex < 0 ||
                _inventory == null || !_inventory.DropItem(_selectedIndex, 1)) return;
            _slotList?.RefreshAll();
            ShowDetails(_selectedIndex);
        }

        private void SortInventory()
        {
            if (_inventory == null) return;
            _inventory.SortItems();
            _selectedIndex = -1;
            _selectedArea = InventorySlotArea.Inventory;
            _slotList?.RefreshAll();
            _slotList?.SetSelected(-1);
            SelectFirstOccupied();
        }

        private void SelectFirstOccupied()
        {
            _selectedIndex = -1;
            _selectedArea = InventorySlotArea.Inventory;
            _slotList?.SetSelected(-1);
            SetEquipmentSelected(-1);
            ClearDetails();
        }

        private void RefreshEquipment()
        {
            if (_equipment == null || itemDB == null) return;
            int count = Mathf.Min(_equipmentSlotViews.Count, _equipment.EquipmentSlotCount);
            for (int i = 0; i < count; i++)
            {
                ItemSlot view = _equipmentSlotViews[i];
                view.Init(_invSwapPublisher, i, InventorySlotArea.Equipment);
                view.ConfigurePlaceholder(GetEquipmentGlyph(_equipment.GetEquipmentSlotType(i)),
                    GetEquipmentLabel(_equipment.GetEquipmentSlotType(i), i));
                InventorySlot slot = _equipment.GetEquipmentSlot(i);
                if (slot == null || slot.IsEmpty) view.Clear();
                else view.Set(slot, itemDB.GetItem(slot.ins.itemId), i);
            }
        }

        private void SetEquipmentSelected(int index)
        {
            for (int i = 0; i < _equipmentSlotViews.Count; i++)
                _equipmentSlotViews[i].SetSelected(i == index);
        }

        private static string GetEquipmentLabel(EquipmentSlotType type, int index)
        {
            if (type == EquipmentSlotType.UpgradeChip)
            {
                int chipNumber = index - 3;
                return L10n.F("k_f69e327f50", chipNumber);
            }
            return type switch
            {
                EquipmentSlotType.Head => L10n.T("k_74eee3ddf2"),
                EquipmentSlotType.Body => L10n.T("k_c6cb974f57"),
                EquipmentSlotType.Legs => L10n.T("k_982037b144"),
                EquipmentSlotType.Feet => L10n.T("k_45a4bd47fd"),
                _ => L10n.T("k_4ce47cf650")
            };
        }

        private static string GetEquipmentGlyph(EquipmentSlotType type) => type switch
        {
            EquipmentSlotType.Head => "◉",
            EquipmentSlotType.Body => "◇",
            EquipmentSlotType.Legs => "Ⅱ",
            EquipmentSlotType.Feet => "⌞",
            EquipmentSlotType.UpgradeChip => "⬡",
            _ => "＋"
        };
    }
}
