using AstraNope.Data.Items;
using AstraNope.Data.Messages;
using AstraNope.Contracts;
using AstraNope.Types.States;
using UnityEngine;

namespace AstraNope.Gameplay.Player
{
    public partial class InventoryController
    {
        public bool SelectHotbar(int index)
        {
            if (_uiState != PlayerUIState.None || index < 0 || index >= HotbarSlotCount) return false;
            _selectedHotbarIndex = index;
            SyncHeldItem();
            _hotbarPublisher?.Publish(new HotbarSelectionMessage(index));
            return true;
        }

        public void CycleHotbar(int direction)
        {
            if (direction == 0 || HotbarSlotCount <= 0) return;
            int next = (_selectedHotbarIndex + (direction > 0 ? -1 : 1) + HotbarSlotCount) % HotbarSlotCount;
            SelectHotbar(next);
        }
        private void SyncHeldItem()
        {
            if (!_itemHolder) _itemHolder = GetComponent<FirstPersonItemHolder>();
            if (!_itemHolder || _selectedHotbarIndex < 0 || _selectedHotbarIndex >= HotbarSlotCount) return;
            var slot = hotbarItems[_selectedHotbarIndex];
            if (slot == null || slot.IsEmpty)
            {
                _itemHolder.Unequip();
                return;
            }

            var item = itemDB.GetItem(slot.ins.itemId);
            if (!_itemHolder.TryEquip(item, slot.ins)) _itemHolder.Unequip();
        }

        private void PublishItemNotification(int itemId, int count, NotificationKind kind)
        {
            if (_notifications == null || itemDB == null) return;
            var item = itemDB.GetItem(itemId);
            string prefix = kind == NotificationKind.ItemAdded ? "+" : "-";
            _notifications.Show(new NotificationMessage(
                item.itemName,
                $"{prefix}{count}",
                AstraNope.UI.Panels.InventoryPanel.GetGlyph(item.itemType),
                kind));
        }

    }
}