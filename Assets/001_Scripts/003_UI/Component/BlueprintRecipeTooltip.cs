using System;
using System.Collections.Generic;
using _001_Scripts.Data.BluePrint;
using _001_Scripts.Data.Item;
using _001_Scripts.Interface;
using UnityEngine;
using UnityEngine.UI;

namespace _001_Scripts.UI.Component
{
    public sealed class BlueprintRecipeTooltip : MonoBehaviour
    {
        [Serializable]
        public sealed class IngredientView
        {
            [SerializeField] private GameObject root;
            [SerializeField] private Image icon;
            [SerializeField] private Text fallbackGlyph;
            [SerializeField] private Text nameLabel;
            [SerializeField] private Text countLabel;

            public IngredientView(GameObject root, Image icon, Text fallbackGlyph, Text nameLabel, Text countLabel)
            {
                this.root = root;
                this.icon = icon;
                this.fallbackGlyph = fallbackGlyph;
                this.nameLabel = nameLabel;
                this.countLabel = countLabel;
            }

            public void Show(Item item, int itemId, int count)
            {
                if (root) root.SetActive(true);
                Sprite sprite = item?.icon;
                if (icon)
                {
                    icon.sprite = sprite;
                    icon.color = sprite ? Color.white : Color.clear;
                    icon.gameObject.SetActive(true);
                }
                if (fallbackGlyph)
                {
                    fallbackGlyph.gameObject.SetActive(!sprite);
                    fallbackGlyph.text = item != null ? InventoryPanel.GetGlyph(item.itemType) : "?";
                }
                if (nameLabel) nameLabel.text = item != null ? item.itemName : $"Item {itemId}";
                if (countLabel) countLabel.text = $"x {count}";
            }

            public void Hide()
            {
                if (root) root.SetActive(false);
            }
        }

        [SerializeField] private RectTransform panel;
        [SerializeField] private RectTransform bounds;
        [SerializeField] private Text title;
        [SerializeField] private Text emptyLabel;
        [SerializeField] private List<IngredientView> ingredients = new();

        public void Configure(RectTransform tooltipPanel, RectTransform clampBounds, Text titleLabel,
            Text noRecipeLabel, List<IngredientView> ingredientViews)
        {
            panel = tooltipPanel;
            bounds = clampBounds;
            title = titleLabel;
            emptyLabel = noRecipeLabel;
            ingredients = ingredientViews ?? new List<IngredientView>();
            Hide();
        }

        public void Show(BluePrint blueprint, IItemCatalog catalog, RectTransform anchor, bool isUnlocked)
        {
            if (!panel || blueprint == null) return;

            if (!isUnlocked)
            {
                if (title) title.text = blueprint.bluePrintName;
                for (int i = 0; i < ingredients.Count; i++) ingredients[i].Hide();
                if (emptyLabel)
                {
                    emptyLabel.text = "아직 청사진이 해금되지 않았습니다";
                    emptyLabel.gameObject.SetActive(true);
                }
                panel.sizeDelta = new Vector2(360f, 122f);
                RevealAt(anchor);
                return;
            }

            int recipeCount = blueprint.recipe?.Count ?? 0;
            int visibleCount = Mathf.Min(recipeCount, ingredients.Count);
            if (title) title.text = $"{blueprint.bluePrintName}  ·  필요 재료";
            if (emptyLabel)
            {
                emptyLabel.text = "필요 재료 없음";
                emptyLabel.gameObject.SetActive(recipeCount == 0);
            }

            for (int i = 0; i < ingredients.Count; i++)
            {
                if (i >= visibleCount)
                {
                    ingredients[i].Hide();
                    continue;
                }

                RecipeEntry entry = blueprint.recipe[i];
                Item item = null;
                catalog?.TryGetItem(entry.item, out item);
                ingredients[i].Show(item, entry.item, entry.count);
            }

            float width = recipeCount == 0 ? 240f : Mathf.Clamp(34f + visibleCount * 138f, 240f, 1160f);
            panel.sizeDelta = new Vector2(width, 158f);
            RevealAt(anchor);
        }

        private void RevealAt(RectTransform anchor)
        {
            panel.gameObject.SetActive(true);
            panel.SetAsLastSibling();
            PositionBy(anchor);
        }

        public void Hide()
        {
            if (panel) panel.gameObject.SetActive(false);
        }

        private void PositionBy(RectTransform anchor)
        {
            if (!anchor || !bounds || panel.parent != bounds) return;
            Canvas canvas = panel.GetComponentInParent<Canvas>();
            Camera camera = canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            Vector3 anchorWorld = anchor.TransformPoint(anchor.rect.center);
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(camera, anchorWorld);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(bounds, screenPoint, camera,
                    out Vector2 localPoint)) return;

            Vector2 desired = localPoint + new Vector2(0f, -142f);
            Rect limit = bounds.rect;
            Vector2 half = panel.sizeDelta * .5f;
            const float margin = 14f;
            desired.x = Mathf.Clamp(desired.x, limit.xMin + half.x + margin, limit.xMax - half.x - margin);
            desired.y = Mathf.Clamp(desired.y, limit.yMin + half.y + margin, limit.yMax - half.y - margin);
            panel.anchoredPosition = desired;
        }
    }
}
