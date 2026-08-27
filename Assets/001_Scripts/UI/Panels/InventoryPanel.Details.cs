using AstraNope.Data.Items;
using AstraNope.Data.Items.Types;
using AstraNope.Data.Messages;
using AstraNope.Contracts;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

using AstraNope.Localization;
namespace AstraNope.UI.Panels
{
    public partial class InventoryPanel
    {
        private void ShowTooltip(int index, InventorySlotArea area)
        {
            if (!tooltipRoot || itemDB == null) return;
            InventorySlot slot = area == InventorySlotArea.Equipment
                ? _equipment?.GetEquipmentSlot(index)
                : _inventory?.GetSlot(index);
            if (slot == null || slot.IsEmpty)
            {
                HideTooltip();
                return;
            }
            Item item = itemDB.GetItem(slot.ins.itemId);
            SetText(tooltipNameText, string.IsNullOrWhiteSpace(item.itemName) ? L10n.F("k_265bb9e1eb", item.itemId) : item.itemName);
            SetText(tooltipTypeText, $"{GetRoleName(item.Role)}  /  {item.ItemGrade.ToString().ToUpperInvariant()}");
            SetText(tooltipDescriptionText, string.IsNullOrWhiteSpace(item.itemDesc)
                ? L10n.T("k_c5001d09f8")
                : item.itemDesc);
            string weightLabel = item.weight.ToString("0.##");
            string clickHint = area == InventorySlotArea.Equipment ? L10n.T("k_4bf89b6be7") : L10n.T("k_db619cd062");
            SetText(tooltipMetaText, item.HasFeature<IEquipmentItem>()
                ? L10n.F("k_be41067bef", clickHint)
                : L10n.F("k_d0cc5f7344", slot.stack, weightLabel));
            tooltipRoot.gameObject.SetActive(true);
            UpdateTooltipPosition();
        }

        private void HideTooltip()
        {
            if (tooltipRoot) tooltipRoot.gameObject.SetActive(false);
        }

        private void UpdateTooltipPosition()
        {
            Vector2 pointer = Mouse.current?.position.ReadValue() ?? Vector2.zero;
            float width = tooltipRoot.rect.width;
            float height = tooltipRoot.rect.height;
            float x = Mathf.Clamp(pointer.x + 18f, 8f, Mathf.Max(8f, Screen.width - width - 8f));
            float y = Mathf.Clamp(pointer.y - 18f, height + 8f, Mathf.Max(height + 8f, Screen.height - 8f));
            tooltipRoot.position = new Vector3(x, y, 0f);
        }
        private void ShowDetails(int index) => ShowDetails(InventorySlotArea.Inventory, index);

        private void ShowDetails(InventorySlotArea area, int index)
        {
            if (_inventory == null || itemDB == null || index < 0)
            {
                ClearDetails();
                return;
            }

            InventorySlot slot;
            if (area == InventorySlotArea.Equipment)
            {
                if (_equipment == null || index >= _equipment.EquipmentSlotCount)
                {
                    ClearDetails();
                    return;
                }
                slot = _equipment.GetEquipmentSlot(index);
            }
            else
            {
                if (index >= _inventory.SlotCount)
                {
                    ClearDetails();
                    return;
                }
                slot = _inventory.GetSlot(index);
            }
            if (slot == null || slot.IsEmpty)
            {
                ClearDetails();
                return;
            }

            var template = itemDB.GetItem(slot.ins.itemId);
            SetText(itemNameText, string.IsNullOrWhiteSpace(template.itemName) ? L10n.F("k_265bb9e1eb", template.itemId) : template.itemName);
            SetText(itemTypeText, GetRoleName(template.Role) + "  /  " + template.ItemGrade.ToString().ToUpperInvariant());
            string templateWeight = template.weight.ToString("0.##");
            SetText(itemQuantityText, L10n.F("k_6890392bd9", slot.stack, templateWeight));
            SetText(itemDescriptionText, string.IsNullOrWhiteSpace(template.itemDesc)
                ? L10n.T("k_87dcb24d2e")
                : template.itemDesc);
            SetText(itemGlyphText, GetGlyph(template.itemType));
            if (useButton) useButton.interactable = area == InventorySlotArea.Inventory && template.HasFeature<IUsable>();
            if (dropButton) dropButton.interactable = area == InventorySlotArea.Inventory;
        }

        private void ClearDetails()
        {
            SetText(itemNameText, L10n.T("k_6f002332f2"));
            SetText(itemTypeText, "NO ITEM SELECTED");
            SetText(itemQuantityText, string.Empty);
            SetText(itemDescriptionText, L10n.T("k_de408783aa"));
            SetText(itemGlyphText, "◇");
            if (useButton) useButton.interactable = false;
            if (dropButton) dropButton.interactable = false;
        }
        private static void SetText(Text target, string value)
        {
            if (target) target.text = value;
        }

        private static string GetTypeName(ItemType type) => type switch
        {
            ItemType.materials => L10n.T("k_df4ee15a02"),
            ItemType.weapon => L10n.T("k_0f0f86540c"),
            ItemType.armor => L10n.T("k_75828c8a69"),
            ItemType.consumable => L10n.T("k_905e5ba805"),
            _ => L10n.T("k_976d37728f")
        };

        private static string GetRoleName(ItemRole role) => role switch
        {
            ItemRole.Tool => L10n.T("k_36c416ead2"),
            ItemRole.Usable => L10n.T("k_a6b5ecb5ee"),
            ItemRole.Equipment => L10n.T("k_4ce47cf650"),
            ItemRole.Material => L10n.T("k_cff206505c"),
            _ => L10n.T("k_2991f61ced")
        };

        public static string GetGlyph(ItemType type) => type switch
        {
            ItemType.materials => "◆",
            ItemType.weapon => "⚔",
            ItemType.armor => "⬡",
            ItemType.consumable => "●",
            _ => "◇"
        };
    }
}