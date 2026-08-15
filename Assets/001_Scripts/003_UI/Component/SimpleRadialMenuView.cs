using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace _001_Scripts.UI.Component
{
    public readonly struct SimpleRadialIngredientData
    {
        public readonly Sprite Icon;
        public readonly string Glyph;
        public readonly string Name;
        public readonly int Count;

        public SimpleRadialIngredientData(Sprite icon, string glyph, string name, int count)
        {
            Icon = icon;
            Glyph = glyph;
            Name = name;
            Count = count;
        }
    }

    public sealed class SimpleRadialRecipeTooltipData
    {
        public readonly string Description;
        public readonly IReadOnlyList<SimpleRadialIngredientData> Ingredients;

        public SimpleRadialRecipeTooltipData(string description,
            IReadOnlyList<SimpleRadialIngredientData> ingredients)
        {
            Description = description;
            Ingredients = ingredients ?? Array.Empty<SimpleRadialIngredientData>();
        }
    }

    public readonly struct SimpleRadialEntry
    {
        public readonly string Id;
        public readonly string Icon;
        public readonly string Label;
        public readonly bool Interactable;
        public readonly Action Selected;
        public readonly string Tooltip;
        public readonly Action SecondarySelected;
        public readonly SimpleRadialRecipeTooltipData RecipeTooltip;

        public SimpleRadialEntry(string id, string icon, string label, Action selected, bool interactable = true,
            string tooltip = null, Action secondarySelected = null,
            SimpleRadialRecipeTooltipData recipeTooltip = null)
        {
            Id = id;
            Icon = icon;
            Label = label;
            Selected = selected;
            Interactable = interactable;
            Tooltip = tooltip;
            SecondarySelected = secondarySelected;
            RecipeTooltip = recipeTooltip;
        }
    }

    [DisallowMultipleComponent]
    public sealed class SimpleRadialMenuView : MonoBehaviour
    {
        private const string RootName = "SimpleRadialRoot";
        private const int NodePoolSize = 24;
        private static readonly Color CenterColor = new(.18f, .075f, .29f, .78f);
        private static readonly Color NodeColor = new(.24f, .10f, .38f, .74f);
        private static readonly Color DisabledColor = new(.105f, .055f, .16f, .52f);
        private static readonly Color StrokeColor = new(.73f, .46f, 1f, .72f);
        private static readonly Color TooltipColor = new(.18f, .18f, .20f, .70f);
        private static readonly Color PinnedColor = new(.12f, .05f, .21f, .76f);

        [SerializeField, Min(1f)] private float radius = 210f;
        [SerializeField, Min(1f)] private float centerSize = 104f;
        [SerializeField, Min(1f)] private float nodeSize = 92f;
        [SerializeField, Min(0f)] private float strokeWidth = 1.5f;
        [SerializeField, Range(.05f, .5f)] private float openDuration = .18f;
        [SerializeField, Range(.05f, .5f)] private float closeDuration = .12f;
        [SerializeField, Range(0f, .1f)] private float nodeDelay = .025f;
        [SerializeField] private Sprite roundedPanelSprite;

        private RectTransform _root;
        private OrganicGradientGraphic _ambient;
        private RectTransform _menu;
        private Transform _nodes;
        private Text _centerIcon;
        private CanvasGroup _rootGroup;
        private Button _outsideButton;
        private Action _outsideClicked;
        private Action _pinnedCleared;
        private Coroutine _animation;
        private readonly List<SimpleRadialNodeView> _nodePool = new(NodePoolSize);
        private RectTransform _tooltip;
        private Text _tooltipText;
        private Text _tooltipIngredientsTitle;
        private RectTransform _tooltipIngredientGrid;
        private readonly List<TooltipIngredientView> _tooltipIngredients = new();
        private RectTransform _pinnedPanel;
        private Text _pinnedTitle;
        private Text _pinnedBody;

        public void SetRoundedPanelSprite(Sprite sprite) => roundedPanelSprite = sprite;

        public void Rebuild(string centerIcon)
        {
            RemoveRoot();
            Build(centerIcon);
        }

        public void Ensure(string centerIcon)
        {
            if (!_root)
            {
                var existing = transform.Find(RootName);
                if (existing)
                {
                    _root = existing as RectTransform;
                    _ambient = existing.Find("OrganicAmbient")?.GetComponent<OrganicGradientGraphic>();
                    _menu = existing.Find("Menu") as RectTransform;
                    _nodes = existing.Find("Menu/Nodes");
                    _centerIcon = existing.Find("Menu/Center/Icon")?.GetComponent<Text>();
                    _rootGroup = existing.GetComponent<CanvasGroup>();
                    _outsideButton = existing.Find("Outside")?.GetComponent<Button>();
                    _tooltip = existing.Find("RecipeTooltip") as RectTransform;
                    _tooltipText = existing.Find("RecipeTooltip/Description")?.GetComponent<Text>();
                    _tooltipIngredientsTitle = existing.Find("RecipeTooltip/IngredientsTitle")?.GetComponent<Text>();
                    _tooltipIngredientGrid = existing.Find("RecipeTooltip/IngredientGrid") as RectTransform;
                    _pinnedPanel = existing.Find("PinnedRecipe") as RectTransform;
                    _pinnedTitle = existing.Find("PinnedRecipe/Title")?.GetComponent<Text>();
                    _pinnedBody = existing.Find("PinnedRecipe/Body")?.GetComponent<Text>();
                }
            }

            CollectTooltipIngredients();
            if (!_root || !_ambient || !_menu || !_nodes || !_centerIcon || !_rootGroup || !_outsideButton || !_tooltip ||
                !_tooltipText || !_tooltipIngredientsTitle || !_tooltipIngredientGrid || _tooltipIngredients.Count < 8 ||
                !_pinnedPanel || !_pinnedTitle || !_pinnedBody) Rebuild(centerIcon);
            else _centerIcon.text = centerIcon;
            CollectNodePool();
            if (_nodePool.Count < NodePoolSize) Rebuild(centerIcon);
            ApplyThemeToExisting();
            BindOutsideClick();
        }

        public void SetOutsideClick(Action callback)
        {
            _outsideClicked = callback;
            BindOutsideClick();
        }

        public void SetPinnedCleared(Action callback) => _pinnedCleared = callback;

        public void PlayOpenAnimation()
        {
            if (!Application.isPlaying)
            {
                SetFinalVisualState();
                return;
            }
            if (_animation != null) StopCoroutine(_animation);
            _animation = StartCoroutine(AnimateOpen());
        }

        public void PlayNodeAnimation()
        {
            if (!Application.isPlaying) return;
            if (_animation != null) StopCoroutine(_animation);
            _animation = StartCoroutine(AnimateNodesOnly());
        }

        public void PlayCloseAnimation(Action finished)
        {
            if (!Application.isPlaying)
            {
                finished?.Invoke();
                return;
            }
            if (_animation != null) StopCoroutine(_animation);
            _animation = StartCoroutine(AnimateClose(finished));
        }

        public void SetEntries(IReadOnlyList<SimpleRadialEntry> entries)
        {
            if (!_nodes) return;
            CollectNodePool();
            for (int i = 0; i < _nodePool.Count; i++) _nodePool[i].Clear();
            HideTooltip();
            if (entries == null || entries.Count == 0) return;

            int visibleCount = Mathf.Min(entries.Count, _nodePool.Count);
            if (entries.Count > _nodePool.Count)
                Debug.LogWarning($"[Radial] {entries.Count} entries exceed the preallocated {_nodePool.Count} node pool.", this);
            for (int i = 0; i < visibleCount; i++)
            {
                var entry = entries[i];
                float angle = 90f - i * 360f / visibleCount;
                float radians = angle * Mathf.Deg2Rad;
                Vector2 position = new(Mathf.Cos(radians) * radius, Mathf.Sin(radians) * radius);
                _nodePool[i].Configure(entry, position, this, NodeColor, DisabledColor);
            }
        }

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
                _tooltipIngredientsTitle.text = "필요한 재료";
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
            _tooltipIngredientsTitle = Label("IngredientsTitle", _tooltip, "필요한 재료", 12,
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
            var name = Label("Name", slot.transform, "재료", 12, TextAnchor.MiddleCenter);
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

        private IEnumerator AnimateOpen()
        {
            _rootGroup.alpha = 0f;
            _menu.localScale = Vector3.one * .86f;
            PrepareNodes();
            float elapsed = 0f;
            while (elapsed < openDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Smooth(elapsed / openDuration);
                _rootGroup.alpha = t;
                _menu.localScale = Vector3.one * Mathf.Lerp(.86f, 1f, t);
                AnimateNodes(elapsed, openDuration);
                yield return null;
            }
            SetFinalVisualState();
            _animation = null;
        }

        private IEnumerator AnimateNodesOnly()
        {
            PrepareNodes();
            float duration = Mathf.Max(.1f, openDuration * .75f);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                AnimateNodes(elapsed, duration);
                yield return null;
            }
            FinishNodes();
            _animation = null;
        }

        private IEnumerator AnimateClose(Action finished)
        {
            float startAlpha = _rootGroup.alpha;
            Vector3 startScale = _menu.localScale;
            float elapsed = 0f;
            while (elapsed < closeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Smooth(elapsed / closeDuration);
                _rootGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
                _menu.localScale = Vector3.Lerp(startScale, Vector3.one * .9f, t);
                yield return null;
            }
            _rootGroup.alpha = 0f;
            _animation = null;
            finished?.Invoke();
        }

        private void PrepareNodes()
        {
            for (int i = 0; i < _nodes.childCount; i++)
            {
                var child = _nodes.GetChild(i);
                child.localScale = Vector3.one * .68f;
                child.GetComponent<CanvasGroup>().alpha = 0f;
            }
        }

        private void AnimateNodes(float elapsed, float duration)
        {
            for (int i = 0; i < _nodes.childCount; i++)
            {
                float t = Smooth((elapsed - i * nodeDelay) / Mathf.Max(.05f, duration * .72f));
                var child = _nodes.GetChild(i);
                child.localScale = Vector3.one * Mathf.Lerp(.68f, 1f, t);
                child.GetComponent<CanvasGroup>().alpha = t;
            }
        }

        private void FinishNodes()
        {
            for (int i = 0; i < _nodes.childCount; i++)
            {
                var child = _nodes.GetChild(i);
                child.localScale = Vector3.one;
                child.GetComponent<CanvasGroup>().alpha = 1f;
            }
        }

        private void SetFinalVisualState()
        {
            if (_rootGroup) _rootGroup.alpha = 1f;
            if (_menu) _menu.localScale = Vector3.one;
            if (_nodes) FinishNodes();
        }

        private static float Smooth(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

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
