using System;
using AstraNope.Data.Databases;
using AstraNope.Contracts;
using UnityEngine;
using UnityEngine.UI;
using BluePrintModel = AstraNope.Data.Blueprints.BluePrint;

using AstraNope.UI.Panels;
using AstraNope.Localization;
namespace AstraNope.UI.Components
{
    public class BlueprintSlot : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private Text iconText;
        [SerializeField] private Text nameText;
        [SerializeField] private Text metaText;
        [SerializeField] private Text stateText;
        [SerializeField] private Button selectButton;

        private BluePrintModel _blueprint;
        private IInventoryService _inventory;

        public BluePrintModel Blueprint => _blueprint;

        public void Init(BluePrintModel blueprint, ItemDataBase itemDatabase, Action<BluePrintModel> onSelected)
        {
            _blueprint = blueprint;
            var result = itemDatabase.GetItem(blueprint.resultCraft);
            if (iconText) iconText.text = InventoryPanel.GetGlyph(result.itemType);
            if (nameText) nameText.text = blueprint.bluePrintName;
            if (metaText) metaText.text = $"LEVEL {blueprint.requiredLevel}   ??  {blueprint.craftTime:0.#} SEC";
            if (stateText) stateText.text = "READY";
            if (selectButton)
            {
                selectButton.onClick.RemoveAllListeners();
                selectButton.onClick.AddListener(() => onSelected(blueprint));
            }
        }

        public void SetSelected(bool selected)
        {
            if (background)
                background.color = selected
                    ? new Color(0.42f, 0.25f, 0.72f, 0.80f)
                    : new Color(0.13f, 0.08f, 0.23f, 0.74f);
            if (stateText)
            {
                stateText.text = selected ? "SELECTED" : "READY";
                stateText.color = selected ? SurvivalUITheme.Cyan : SurvivalUITheme.TextMuted;
            }
        }

        public void RefreshAffordability(IInventoryService inventory)
        {
            if (_blueprint == null) return;
            bool affordable = true;
            for (int i = 0; i < _blueprint.recipe.Count; i++)
                affordable &= inventory.HasItem(_blueprint.recipe[i].item, _blueprint.recipe[i].count);
            if (stateText && !affordable)
            {
                stateText.text = L10n.T("k_c54bd3234e");
                stateText.color = SurvivalUITheme.Danger;
            }
        }
    }
}
