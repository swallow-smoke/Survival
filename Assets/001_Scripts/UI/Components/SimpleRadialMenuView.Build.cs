using System;
using UnityEngine;
using UnityEngine.UI;

namespace AstraNope.UI.Components
{
    public sealed partial class SimpleRadialMenuView
    {
        private void Build(string centerIcon)
        {
            _root = Rect(RootName, transform);
            Stretch(_root);
            _rootGroup = _root.gameObject.AddComponent<CanvasGroup>();

            var ambient = Rect("OrganicAmbient", _root);
            Stretch(ambient);
            _ambient = ambient.gameObject.AddComponent<OrganicGradientGraphic>();
            _ambient.Configure(new Color(.105f, .04f, .19f, .34f), new Color(.008f, .003f, .03f, .62f),
                new Color(.66f, .34f, 1f, .10f), new Color(.18f, .50f, 1f, .065f));

            var outside = new GameObject("Outside", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(Button));
            outside.layer = 5;
            outside.transform.SetParent(_root, false);
            var outsideRect = outside.GetComponent<RectTransform>();
            Stretch(outsideRect);
            var outsideImage = outside.GetComponent<Image>();
            outsideImage.color = new Color(0f, 0f, 0f, .001f);
            _outsideButton = outside.GetComponent<Button>();
            _outsideButton.targetGraphic = outsideImage;
            _outsideButton.transition = Selectable.Transition.None;

            _menu = Rect("Menu", _root);
            Anchor(_menu, new Vector2(.5f, .5f), new Vector2(640f, 640f), Vector2.zero);
            _nodes = Rect("Nodes", _menu);
            Stretch((RectTransform)_nodes);

            _nodePool.Clear();
            for (int i = 0; i < NodePoolSize; i++)
                _nodePool.Add(CreateNodeShell($"PooledNode_{i:00}"));

            var center = Circle("Center", _menu, centerSize, CenterColor);
            AddStroke(center);
            center.raycastTarget = true;
            _centerIcon = Label("Icon", center.transform, centerIcon, 34, TextAnchor.MiddleCenter);
            Stretch(_centerIcon.rectTransform);
            BuildTooltip();
            BuildPinnedPanel();
            BindOutsideClick();
            SetFinalVisualState();
        }

        private SimpleRadialNodeView CreateNodeShell(string name)
        {
            var circle = Circle(name, _nodes, nodeSize, NodeColor);
            var button = circle.gameObject.AddComponent<Button>();
            button.targetGraphic = circle;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.18f, 1.08f, 1.25f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color(.88f, .72f, 1f, 1f);
            colors.disabledColor = new Color(.62f, .54f, .68f, .68f);
            colors.fadeDuration = .08f;
            button.colors = colors;
            circle.gameObject.AddComponent<CanvasGroup>();
            AddStroke(circle);
            var label = Label("Content", circle.transform, string.Empty, 13, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform);
            var view = circle.gameObject.AddComponent<SimpleRadialNodeView>();
            view.BindSerializedReferences(circle, button, label);
            view.Clear();
            return view;
        }
        private void CollectNodePool()
        {
            _nodePool.Clear();
            if (!_nodes) return;
            for (int i = 0; i < _nodes.childCount; i++)
            {
                var view = _nodes.GetChild(i).GetComponent<SimpleRadialNodeView>();
                if (view) _nodePool.Add(view);
            }
        }
        private void ApplyThemeToExisting()
        {
            var center = _menu ? _menu.Find("Center")?.GetComponent<RadialCircleGraphic>() : null;
            if (center) center.color = CenterColor;
            if (_tooltip)
            {
                var image = _tooltip.GetComponent<Image>();
                if (image) image.color = TooltipColor;
            }
            if (_pinnedPanel)
            {
                var image = _pinnedPanel.GetComponent<Image>();
                if (image) image.color = PinnedColor;
                var unpin = _pinnedPanel.Find("Unpin")?.GetComponent<Image>();
                if (unpin) unpin.color = new Color(.28f, .12f, .42f, .76f);
            }
            foreach (var outline in _root.GetComponentsInChildren<Outline>(true))
            {
                outline.effectColor = StrokeColor;
                outline.effectDistance = Vector2.one * strokeWidth;
                outline.useGraphicAlpha = true;
            }
        }

        private void AddStroke(Graphic graphic)
        {
            var outline = graphic.gameObject.AddComponent<Outline>();
            outline.effectColor = StrokeColor;
            outline.effectDistance = Vector2.one * strokeWidth;
            outline.useGraphicAlpha = true;
        }

        private void BindOutsideClick()
        {
            if (!_outsideButton) return;
            _outsideButton.onClick.RemoveListener(InvokeOutsideClick);
            _outsideButton.onClick.AddListener(InvokeOutsideClick);
        }

        private void InvokeOutsideClick() => _outsideClicked?.Invoke();
        private static RadialCircleGraphic Circle(string name, Transform parent, float size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RadialCircleGraphic));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            var circle = go.GetComponent<RadialCircleGraphic>();
            circle.color = color;
            circle.InnerRadius = 0f;
            Anchor(circle.rectTransform, new Vector2(.5f, .5f), Vector2.one * size, Vector2.zero);
            return circle;
        }

        private static Text Label(string name, Transform parent, string value, int size, TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.color = Color.white;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }
        private void RemoveRoot()
        {
            if (_animation != null)
            {
                StopCoroutine(_animation);
                _animation = null;
            }
            var existing = transform.Find(RootName);
            if (!existing) return;
            existing.gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(existing.gameObject);
            else DestroyImmediate(existing.gameObject);
            _root = null;
            _menu = null;
            _nodes = null;
            _centerIcon = null;
            _rootGroup = null;
            _outsideButton = null;
            _nodePool.Clear();
            _tooltip = null;
            _tooltipText = null;
            _tooltipIngredientsTitle = null;
            _tooltipIngredientGrid = null;
            _tooltipIngredients.Clear();
            _pinnedPanel = null;
            _pinnedTitle = null;
            _pinnedBody = null;
        }
        private Image Panel(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            if (roundedPanelSprite)
            {
                image.sprite = roundedPanelSprite;
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = 1f;
            }
            return image;
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Stretch(RectTransform rect, float inset)
        {
            Stretch(rect);
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private static void Anchor(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 position)
            => Anchor(rect, anchor, size, position, new Vector2(.5f, .5f));

        private static void Anchor(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 position, Vector2 pivot)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            rect.localScale = Vector3.one;
        }
    }
}