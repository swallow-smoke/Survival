using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using AstraNope.Localization;
namespace AstraNope.UI.Components
{
    public sealed partial class SimpleRadialMenuView
    {
        public void ShowTooltip(string text, Vector2 screenPosition)
        {
            if (!_tooltip || !_tooltipText || string.IsNullOrWhiteSpace(text)) return;
            _tooltipText.text = text;
            _tooltipText.color = Color.white;
            if (_tooltipIngredientsTitle) _tooltipIngredientsTitle.gameObject.SetActive(false);
            for (int i = 0; i < _tooltipIngredients.Count; i++) _tooltipIngredients[i].Hide();
            _tooltip.sizeDelta = new Vector2(360f, 150f);
            RevealTooltip(screenPosition);
        }

        public void ShowRecipeTooltip(SimpleRadialRecipeTooltipData data, Vector2 screenPosition)
        {
            if (!_tooltip || !_tooltipText || data == null) return;
            _tooltipText.text = data.Description;
            _tooltipText.color = Color.white;
            if (_tooltipIngredientsTitle)
            {
                _tooltipIngredientsTitle.text = L10n.T("k_74405ad23b");
                _tooltipIngredientsTitle.gameObject.SetActive(true);
            }
            int visible = Mathf.Min(data.Ingredients.Count, _tooltipIngredients.Count);
            for (int i = 0; i < _tooltipIngredients.Count; i++)
            {
                if (i < visible) _tooltipIngredients[i].Show(data.Ingredients[i]);
                else _tooltipIngredients[i].Hide();
            }
            float width = visible == 0 ? 380f : Mathf.Clamp(34f + visible * 138f, 380f, 1140f);
            _tooltip.sizeDelta = new Vector2(width, 250f);
            RevealTooltip(screenPosition);
        }

        private void RevealTooltip(Vector2 screenPosition)
        {
            _tooltip.gameObject.SetActive(true);
            Camera eventCamera = GetComponentInParent<Canvas>()?.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : GetComponentInParent<Canvas>()?.worldCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screenPosition, eventCamera,
                    out Vector2 local)) return;
            Vector2 half = _tooltip.sizeDelta * .5f;
            Rect bounds = _root.rect;
            local += new Vector2(half.x + 18f, -half.y - 18f);
            local.x = Mathf.Clamp(local.x, bounds.xMin + half.x + 8f, bounds.xMax - half.x - 8f);
            local.y = Mathf.Clamp(local.y, bounds.yMin + half.y + 8f, bounds.yMax - half.y - 8f);
            _tooltip.anchoredPosition = local;
            _tooltip.SetAsLastSibling();
        }

        public void HideTooltip()
        {
            if (_tooltip) _tooltip.gameObject.SetActive(false);
        }

        public void SetPinnedRecipe(string title, string body)
        {
            if (!_pinnedPanel || !_pinnedTitle || !_pinnedBody) return;
            _pinnedTitle.text = title;
            _pinnedBody.text = body;
            _pinnedPanel.gameObject.SetActive(true);
        }

        public void ClearPinnedRecipe()
        {
            if (_pinnedPanel) _pinnedPanel.gameObject.SetActive(false);
            _pinnedCleared?.Invoke();
        }
        private void BuildTooltip()
        {
            var image = Panel("RecipeTooltip", _root, TooltipColor);
            _tooltip = image.rectTransform;
            Anchor(_tooltip, new Vector2(.5f, .5f), new Vector2(380f, 250f), Vector2.zero);
            AddStroke(image);
            var shadow = image.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, .30f);
            shadow.effectDistance = new Vector2(0f, -6f);
            shadow.useGraphicAlpha = true;
            _tooltipText = Label("Description", _tooltip, string.Empty, 14, TextAnchor.UpperLeft);
            Anchor(_tooltipText.rectTransform, new Vector2(0f, 1f), new Vector2(348f, 62f),
                new Vector2(16f, -12f), new Vector2(0f, 1f));
            _tooltipText.color = Color.white;
            _tooltipIngredientsTitle = Label("IngredientsTitle", _tooltip, L10n.T("k_74405ad23b"), 12,
                TextAnchor.MiddleLeft);
            Anchor(_tooltipIngredientsTitle.rectTransform, new Vector2(0f, 1f), new Vector2(330f, 24f),
                new Vector2(16f, -76f), new Vector2(0f, 1f));

            _tooltipIngredientGrid = Rect("IngredientGrid", _tooltip);
            _tooltipIngredientGrid.anchorMin = Vector2.zero;
            _tooltipIngredientGrid.anchorMax = Vector2.one;
            _tooltipIngredientGrid.offsetMin = new Vector2(12f, 12f);
            _tooltipIngredientGrid.offsetMax = new Vector2(-12f, -102f);
            var layout = _tooltipIngredientGrid.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlHeight = layout.childControlWidth = false;
            layout.childForceExpandHeight = layout.childForceExpandWidth = false;
            _tooltipIngredients.Clear();
            for (int i = 0; i < 8; i++) _tooltipIngredients.Add(BuildTooltipIngredient(i));
            image.raycastTarget = false;
            _tooltip.gameObject.SetActive(false);
        }

        private TooltipIngredientView BuildTooltipIngredient(int index)
        {
            var slot = Panel($"Ingredient_{index:00}", _tooltipIngredientGrid,
                new Color(.10f, .10f, .12f, .76f));
            slot.rectTransform.sizeDelta = new Vector2(128f, 128f);
            slot.raycastTarget = false;
            AddStroke(slot);
            var imageBackground = Panel("ImageBackground", slot.transform, new Color(.22f, .22f, .25f, .76f));
            Anchor(imageBackground.rectTransform, new Vector2(.5f, 1f), new Vector2(62f, 62f),
                new Vector2(0f, -7f), new Vector2(.5f, 1f));
            imageBackground.raycastTarget = false;
            AddStroke(imageBackground);
            var icon = Panel("Image", imageBackground.transform, Color.clear);
            Stretch(icon.rectTransform, 7f);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            var glyph = Label("FallbackGlyph", imageBackground.transform, "?", 24, TextAnchor.MiddleCenter);
            Stretch(glyph.rectTransform, 5f);
            var name = Label("Name", slot.transform, L10n.T("k_cff206505c"), 12, TextAnchor.MiddleCenter);
            Anchor(name.rectTransform, new Vector2(.5f, 0f), new Vector2(118f, 28f), new Vector2(0f, 30f),
                new Vector2(.5f, 0f));
            var count = Label("Count", slot.transform, "x 1", 13, TextAnchor.MiddleCenter);
            Anchor(count.rectTransform, new Vector2(.5f, 0f), new Vector2(118f, 24f), new Vector2(0f, 6f),
                new Vector2(.5f, 0f));
            count.color = new Color(.88f, .80f, 1f, 1f);
            slot.gameObject.SetActive(false);
            return new TooltipIngredientView(slot.gameObject, icon, glyph, name, count);
        }

        private void CollectTooltipIngredients()
        {
            _tooltipIngredients.Clear();
            if (!_tooltipIngredientGrid) return;
            for (int i = 0; i < _tooltipIngredientGrid.childCount; i++)
            {
                Transform slot = _tooltipIngredientGrid.GetChild(i);
                var icon = slot.Find("ImageBackground/Image")?.GetComponent<Image>();
                var glyph = slot.Find("ImageBackground/FallbackGlyph")?.GetComponent<Text>();
                var name = slot.Find("Name")?.GetComponent<Text>();
                var count = slot.Find("Count")?.GetComponent<Text>();
                if (icon && glyph && name && count)
                    _tooltipIngredients.Add(new TooltipIngredientView(slot.gameObject, icon, glyph, name, count));
            }
        }

        private void BuildPinnedPanel()
        {
            var image = Panel("PinnedRecipe", _root, PinnedColor);
            _pinnedPanel = image.rectTransform;
            _pinnedPanel.anchorMin = _pinnedPanel.anchorMax = _pinnedPanel.pivot = new Vector2(1f, 1f);
            _pinnedPanel.sizeDelta = new Vector2(350f, 250f);
            _pinnedPanel.anchoredPosition = new Vector2(-24f, -24f);
            AddStroke(image);
            _pinnedTitle = Label("Title", _pinnedPanel, string.Empty, 18, TextAnchor.UpperLeft);
            Anchor(_pinnedTitle.rectTransform, new Vector2(0f, 1f), new Vector2(294f, 40f), new Vector2(16f, -14f),
                new Vector2(0f, 1f));
            _pinnedBody = Label("Body", _pinnedPanel, string.Empty, 14, TextAnchor.UpperLeft);
            Anchor(_pinnedBody.rectTransform, new Vector2(0f, 1f), new Vector2(318f, 180f), new Vector2(16f, -58f),
                new Vector2(0f, 1f));
            var close = Panel("Unpin", _pinnedPanel, new Color(.28f, .12f, .42f, .76f));
            Anchor(close.rectTransform, new Vector2(1f, 1f), new Vector2(34f, 34f), new Vector2(-10f, -10f),
                new Vector2(1f, 1f));
            var button = close.gameObject.AddComponent<Button>();
            button.targetGraphic = close;
            button.onClick.AddListener(ClearPinnedRecipe);
            var x = Label("Label", close.transform, "×", 20, TextAnchor.MiddleCenter);
            Stretch(x.rectTransform);
            _pinnedPanel.gameObject.SetActive(false);
        }
        private sealed class TooltipIngredientView
        {
            private readonly GameObject _root;
            private readonly Image _icon;
            private readonly Text _glyph;
            private readonly Text _name;
            private readonly Text _count;

            public TooltipIngredientView(GameObject root, Image icon, Text glyph, Text name, Text count)
            {
                _root = root;
                _icon = icon;
                _glyph = glyph;
                _name = name;
                _count = count;
            }

            public void Show(SimpleRadialIngredientData data)
            {
                _root.SetActive(true);
                _icon.sprite = data.Icon;
                _icon.color = data.Icon ? Color.white : Color.clear;
                _glyph.gameObject.SetActive(!data.Icon);
                _glyph.text = string.IsNullOrWhiteSpace(data.Glyph) ? "?" : data.Glyph;
                _name.text = data.Name;
                _count.text = $"x {Mathf.Max(0, data.Count)}";
            }

            public void Hide() => _root.SetActive(false);
        }
    }
}