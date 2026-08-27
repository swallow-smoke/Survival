using System;
using AstraNope.UI.Base;
using AstraNope.Data.Messages;
using AstraNope.Data.Databases;
using AstraNope.Contracts;
using AstraNope.UI.Components;
using MessagePipe;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace AstraNope.UI.Panels
{
    public class HUDPanel : PanelBase
    {
        [Header("Oxygen")]
        [SerializeField] private RectTransform oxygenFill;
        [SerializeField] private Text oxygenValue;

        [Header("Health")]
        [SerializeField] private RectTransform healthFill;
        [SerializeField] private Text healthValue;

        [Header("Hunger")]
        [SerializeField] private RectTransform hungerFill;
        [SerializeField] private Text hungerValue;

        [Header("Hydration")]
        [SerializeField] private RectTransform hydrationFill;
        [SerializeField] private Text hydrationValue;

        [Header("Display")]
        [SerializeField, Range(0f, 1f)] private float warningThreshold = .25f;
        [SerializeField] private Color warningColor = new(1f, .42f, .72f, 1f);
        [SerializeField, HideInInspector] private int editorStyleVersion;

        [Header("Hotbar")]
        [SerializeField] private ItemDataBase itemDB;
        [SerializeField] private RectTransform hotbarRoot;

        private readonly List<Image> _hotbarBackgrounds = new();
        private readonly List<ItemSlot> _hotbarSlots = new();
        private readonly List<Text> _hotbarGlyphs = new();
        private IHotbarReader _hotbar;
        private IHotbarActions _hotbarActions;
        private IPublisher<InventorySwapMessage> _swapPublisher;
        private LeftNotificationFeed _notifications;

        private IDisposable _bag;

        private new void Awake()
        {
            base.Awake();
            if (!itemDB) itemDB = Resources.Load<ItemDataBase>("Data/ItemDataBase");
            NormalizeRootScale();
            ApplyFont();
            BuildHotbar();
            _notifications = GetComponent<LeftNotificationFeed>();
            if (!_notifications) _notifications = gameObject.AddComponent<LeftNotificationFeed>();
            _notifications.EnsureView();
        }

        private void OnValidate()
        {
            NormalizeRootScale();
            ApplyFont();
        }

        private void NormalizeRootScale()
        {
            if (transform is RectTransform rect && rect.localScale != Vector3.one)
                rect.localScale = Vector3.one;
        }

        private void ApplyFont()
        {
            foreach (var label in GetComponentsInChildren<Text>(true))
                label.font = SurvivalUITheme.Font;
        }

        [Inject]
        public void Constructor(ISubscriber<PlayerStatMessage> playerStatSubscriber,
            ISubscriber<InventoryChangedMessage> inventorySubscriber,
            ISubscriber<HotbarSelectionMessage> hotbarSubscriber,
            ISubscriber<NotificationMessage> notificationSubscriber,
            IHotbarReader hotbar,
            IHotbarActions hotbarActions,
            IPublisher<InventorySwapMessage> swapPublisher)
        {
            _bag?.Dispose();
            _hotbar = hotbar;
            _hotbarActions = hotbarActions;
            _swapPublisher = swapPublisher;
            for (int i = 0; i < _hotbarSlots.Count; i++)
                _hotbarSlots[i].Init(_swapPublisher, i, InventorySlotArea.Hotbar);
            var builder = DisposableBag.CreateBuilder();
            builder.Add(playerStatSubscriber.Subscribe(UIUpdate));
            builder.Add(inventorySubscriber.Subscribe(_ => RefreshHotbar()));
            builder.Add(hotbarSubscriber.Subscribe(_ => RefreshHotbar()));
            builder.Add(notificationSubscriber.Subscribe(message => _notifications?.Enqueue(message)));
            _bag = builder.Build();
            RefreshHotbar();
        }

        private void BuildHotbar()
        {
            if (!hotbarRoot)
            {
                var rootObject = new GameObject("HotbarRoot", typeof(RectTransform), typeof(Canvas),
                    typeof(GraphicRaycaster), typeof(HorizontalLayoutGroup));
                hotbarRoot = rootObject.GetComponent<RectTransform>();
                hotbarRoot.SetParent(transform, false);
                hotbarRoot.anchorMin = hotbarRoot.anchorMax = new Vector2(.5f, 0f);
                hotbarRoot.pivot = new Vector2(.5f, 0f);
                hotbarRoot.anchoredPosition = new Vector2(0f, 24f);
                hotbarRoot.sizeDelta = new Vector2(536f, 62f);
                var layout = rootObject.GetComponent<HorizontalLayoutGroup>();
                layout.spacing = 6f;
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.childControlWidth = layout.childControlHeight = false;
            }

            var hotbarCanvas = hotbarRoot.GetComponent<Canvas>();
            if (!hotbarCanvas) hotbarCanvas = hotbarRoot.gameObject.AddComponent<Canvas>();
            hotbarCanvas.overrideSorting = true;
            hotbarCanvas.sortingOrder = 160;
            if (!hotbarRoot.GetComponent<GraphicRaycaster>())
                hotbarRoot.gameObject.AddComponent<GraphicRaycaster>();

            _hotbarBackgrounds.Clear();
            _hotbarSlots.Clear();
            _hotbarGlyphs.Clear();
            for (int i = hotbarRoot.childCount - 1; i >= 0; i--)
            {
                var child = hotbarRoot.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
            }

            const int visibleSlots = 8;
            for (int i = 0; i < visibleSlots; i++)
            {
                int slotIndex = i;
                var slot = new GameObject($"Slot {i + 1}", typeof(RectTransform), typeof(Image),
                    typeof(Outline), typeof(CanvasGroup), typeof(ItemSlot));
                var rect = slot.GetComponent<RectTransform>();
                rect.SetParent(hotbarRoot, false);
                rect.sizeDelta = new Vector2(62f, 62f);
                var image = slot.GetComponent<Image>();
                image.color = new Color(.05f, .07f, .1f, .68f);
                image.raycastTarget = true;
                var outline = slot.GetComponent<Outline>();
                outline.effectColor = new Color(.75f, .85f, 1f, .35f);
                outline.effectDistance = new Vector2(1f, -1f);

                var labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
                var labelRect = labelObject.GetComponent<RectTransform>();
                labelRect.SetParent(rect, false);
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
                var label = labelObject.GetComponent<Text>();
                label.font = SurvivalUITheme.Font;
                label.fontSize = 16;
                label.alignment = TextAnchor.MiddleCenter;
                label.color = Color.white;
                label.raycastTarget = false;

                var countObject = new GameObject("Count", typeof(RectTransform), typeof(Text));
                var countRect = countObject.GetComponent<RectTransform>();
                countRect.SetParent(rect, false);
                countRect.anchorMin = countRect.anchorMax = new Vector2(1f, 0f);
                countRect.pivot = new Vector2(1f, 0f);
                countRect.anchoredPosition = new Vector2(-5f, 3f);
                countRect.sizeDelta = new Vector2(36f, 18f);
                var count = countObject.GetComponent<Text>();
                count.font = SurvivalUITheme.Font;
                count.fontSize = 12;
                count.alignment = TextAnchor.LowerRight;
                count.color = Color.white;
                count.raycastTarget = false;

                var keyObject = new GameObject("Key", typeof(RectTransform), typeof(Text));
                var keyRect = keyObject.GetComponent<RectTransform>();
                keyRect.SetParent(rect, false);
                keyRect.anchorMin = keyRect.anchorMax = new Vector2(0f, 1f);
                keyRect.pivot = new Vector2(0f, 1f);
                keyRect.anchoredPosition = new Vector2(5f, -3f);
                keyRect.sizeDelta = new Vector2(18f, 18f);
                var key = keyObject.GetComponent<Text>();
                key.font = SurvivalUITheme.Font;
                key.fontSize = 11;
                key.text = (i + 1).ToString();
                key.color = SurvivalUITheme.TextMuted;
                key.raycastTarget = false;

                _hotbarBackgrounds.Add(image);
                _hotbarGlyphs.Add(label);
                var itemSlot = slot.GetComponent<ItemSlot>();
                itemSlot.Configure(image, label, null, count, index => _hotbarActions?.SelectHotbar(index));
                if (_swapPublisher != null)
                    itemSlot.Init(_swapPublisher, slotIndex, InventorySlotArea.Hotbar);
                _hotbarSlots.Add(itemSlot);
            }
        }

        private void RefreshHotbar()
        {
            if (_hotbar == null || _hotbarSlots.Count == 0) return;
            for (int i = 0; i < _hotbarSlots.Count; i++)
            {
                bool active = i < _hotbar.HotbarSlotCount;
                _hotbarBackgrounds[i].gameObject.SetActive(active);
                if (!active) continue;
                _hotbarBackgrounds[i].color = i == _hotbar.SelectedHotbarIndex
                    ? new Color(.32f, .52f, .78f, .88f)
                    : new Color(.05f, .07f, .1f, .68f);
                var slot = _hotbar.GetHotbarSlot(i);
                if (slot == null || slot.IsEmpty)
                {
                    _hotbarSlots[i].Clear();
                    _hotbarGlyphs[i].text = string.Empty;
                    _hotbarSlots[i].SetSelected(i == _hotbar.SelectedHotbarIndex);
                    continue;
                }

                var item = itemDB ? itemDB.GetItem(slot.ins.itemId) : null;
                if (item != null) _hotbarSlots[i].Set(slot, item, i);
                else _hotbarSlots[i].Clear();
                _hotbarSlots[i].SetSelected(i == _hotbar.SelectedHotbarIndex);
            }
        }

        private void UIUpdate(PlayerStatMessage msg)
        {
            UpdateStat(healthFill, healthValue, msg.hp);
            UpdateStat(hungerFill, hungerValue, msg.hungry);
            UpdateStat(hydrationFill, hydrationValue, msg.water);
            UpdateStat(oxygenFill, oxygenValue, msg.oxygen);
        }

        private void UpdateStat(RectTransform fill, Text valueLabel, float value)
        {
            float normalized = Mathf.Clamp01(value / 100f);
            if (fill)
            {
                fill.anchorMax = new Vector2(normalized, 1f);
                fill.offsetMax = Vector2.zero;
            }

            if (!valueLabel) return;
            valueLabel.text = $"{Mathf.RoundToInt(value)}%";
            var fillImage = fill ? fill.GetComponent<Image>() : null;
            var normalColor = fillImage ? fillImage.color : Color.white;
            valueLabel.color = normalized <= warningThreshold ? warningColor : normalColor;
        }

        private void OnDestroy() => _bag?.Dispose();
    }

    public static class SurvivalUITheme
    {
        public static readonly Color Cyan = new(.66f, .46f, 1f, 1f);
        public static readonly Color Border = new(.82f, .72f, 1f, .42f);
        public static readonly Color Danger = new(1f, .42f, .72f, 1f);
        public static readonly Color TextMuted = new(.70f, .63f, .82f, 1f);

        private static Font _font;

        public static Font Font
        {
            get
            {
                if (_font) return _font;
                _font = Font.CreateDynamicFontFromOSFont(
                    new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Noto Sans CJK KR", "Arial" }, 32);
                return _font;
            }
        }

        public static void ConfigureCanvas(GameObject target, float width, float height)
        {
            var scaler = target.GetComponent<CanvasScaler>();
            if (!scaler) return;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(width, height);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = .5f;
        }
    }
}
