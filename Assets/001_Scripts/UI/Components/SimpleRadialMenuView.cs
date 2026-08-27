using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AstraNope.UI.Components
{
    [DisallowMultipleComponent]
    public sealed partial class SimpleRadialMenuView : MonoBehaviour
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
    }
}