using System;
using System.Text;
using _001_Scripts.Data.SOJ;
using _001_Scripts.Interface;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BluePrint = _001_Scripts.Data.BluePrint.BluePrint;

namespace _001_Scripts.UI.Component
{
    public class BlueprintSlot : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text ingredientText;
        [SerializeField] private Button craftButton;

        private BluePrint _bluePrint;

        public void Init(BluePrint bp, ItemDataBase itemDB, Action<string> onCraft)
        {
            _bluePrint = bp;

            nameText.text = bp.bluePrintName;

            var sb = new StringBuilder();
            for (int i = 0; i < bp.recipe.Count; i++)
            {
                var entry = bp.recipe[i];
                if (i > 0) sb.Append('\n');
                sb.Append(itemDB.GetItem(entry.item).itemName).Append(" x").Append(entry.count);
            }
            ingredientText.text = sb.ToString();

            craftButton.onClick.RemoveAllListeners();
            craftButton.onClick.AddListener(() => onCraft(bp.bluePrintName));
        }

        public void RefreshAffordability(IInventoryService inv)
        {
            if (_bluePrint == null) return;

            var affordable = true;
            for (int i = 0; i < _bluePrint.recipe.Count; i++)
            {
                var entry = _bluePrint.recipe[i];
                if (inv.HasItem(entry.item, entry.count)) continue;
                affordable = false;
                break;
            }

            craftButton.interactable = affordable;
        }
    }
}
