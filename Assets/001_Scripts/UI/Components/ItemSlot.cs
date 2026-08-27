using System;
using AstraNope.Data.Items;
using AstraNope.Data.Messages;
using AstraNope.Data.Items.Types;
using MessagePipe;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using AstraNope.UI.Panels;
using AstraNope.Localization;
namespace AstraNope.UI.Components
{
    [RequireComponent(typeof(CanvasGroup))]
    public class ItemSlot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        [SerializeField] private Image itemImage;
        [SerializeField] private string itemName;
        [SerializeField] private string itemDesc;
        [SerializeField] private string itemType;
        [SerializeField] private TextMeshProUGUI stock;
        [SerializeField] private Image durability;
        [SerializeField] private int index;
        [SerializeField] private InventorySlotArea area;

        [Header("Serialized UGUI")]
        [SerializeField] private Image background;
        [SerializeField] private Text glyphText;
        [SerializeField] private Text nameText;
        [SerializeField] private Text countText;
        [SerializeField] private string emptyGlyph = "＋";
        [SerializeField] private string emptyName = "";

        private Image _background;
        private Text _glyph;
        private Text _runtimeName;
        private Text _runtimeCount;
        private CanvasGroup _canvasGroup;
        private IPublisher<InventorySwapMessage> _publisher;
        private RectTransform _rectTransform;
        private Action<int> _onSelected;
        private Action<int, InventorySlotArea> _onHover;
        private Action _onHoverExit;
        private Vector2 _originPosition;
        private bool _occupied;
        private bool _selected;

        public static ItemSlot DragSlot;

        public void Configure(Image background, Text glyph, Text nameLabel, Text countLabel, Action<int> onSelected)
        {
            _background = background;
            _glyph = glyph;
            _runtimeName = nameLabel;
            _runtimeCount = countLabel;
            _onSelected = onSelected;
        }

        public void ConfigureSelection(Action<int> onSelected)
        {
            _background = background ? background : GetComponent<Image>();
            _glyph = glyphText;
            _runtimeName = nameText;
            _runtimeCount = countText;
            _onSelected = onSelected;
        }

        public void ConfigurePlaceholder(string glyph, string label)
        {
            emptyGlyph = glyph;
            emptyName = label;
            if (!_occupied) Clear();
        }

        public void ConfigureTooltip(Action<int, InventorySlotArea> onHover, Action onHoverExit)
        {
            _onHover = onHover;
            _onHoverExit = onHoverExit;
        }

        public void Init(IPublisher<InventorySwapMessage> publisher, int slotIndex = -1,
            InventorySlotArea slotArea = InventorySlotArea.Inventory)
        {
            _publisher = publisher;
            if (slotIndex >= 0) index = slotIndex;
            area = slotArea;
        }

        public void Set(InventorySlot slot, Item item, int slotIndex)
        {
            index = slotIndex;
            itemName = item.itemName;
            itemDesc = item.itemDesc;
            itemType = item.Role.ToString();
            _occupied = true;

            if (itemImage)
            {
                itemImage.enabled = itemImage.sprite != null;
                itemImage.preserveAspect = true;
            }

            if (item.TryGetFeature<IEquippable>(out var equippable) && durability)
            {
                float maximum = equippable.MaxDurability;
                durability.fillAmount = maximum <= 0 ? 0 : slot.ins.durability / maximum;
                if (stock) stock.gameObject.SetActive(false);
                durability.gameObject.SetActive(true);
            }
            else
            {
                if (stock)
                {
                    stock.text = slot.stack > 1 ? $"x{slot.stack}" : "";
                    stock.gameObject.SetActive(true);
                }
                if (durability) durability.gameObject.SetActive(false);
            }

            if (_glyph)
            {
                _glyph.text = InventoryPanel.GetGlyph(item.itemType);
                _glyph.color = GetItemColor(item.ItemGrade);
            }
            if (_runtimeName)
                _runtimeName.text = string.IsNullOrWhiteSpace(item.itemName) ? L10n.F("k_265bb9e1eb", item.itemId) : item.itemName;
            if (_runtimeCount)
                _runtimeCount.text = slot.stack > 1 ? $"×{slot.stack}" : "";
            ApplyVisualState();
        }

        public void Clear()
        {
            itemName = "";
            itemDesc = "";
            itemType = "";
            _occupied = false;
            if (itemImage) itemImage.enabled = false;
            if (stock)
            {
                stock.text = "";
                stock.gameObject.SetActive(false);
            }
            if (durability) durability.gameObject.SetActive(false);
            if (_glyph)
            {
                _glyph.text = string.IsNullOrWhiteSpace(emptyGlyph) ? "＋" : emptyGlyph;
                _glyph.color = new Color(0.32f, 0.48f, 0.50f, 0.45f);
            }
            if (_runtimeName) _runtimeName.text = emptyName;
            if (_runtimeCount) _runtimeCount.text = "";
            ApplyVisualState();
        }

        public void SetSelected(bool selected)
        {
            _selected = selected;
            ApplyVisualState();
        }

        private void ApplyVisualState()
        {
            if (!_background) return;
            _background.color = _selected
                ? new Color(0.42f, 0.25f, 0.72f, 0.80f)
                : _occupied
                    ? new Color(0.13f, 0.08f, 0.23f, 0.74f)
                    : new Color(0.08f, 0.05f, 0.15f, 0.70f);
        }

        private static Color GetItemColor(ItemGrade grade) => grade switch
        {
            ItemGrade.rare => new Color(0.62f, 0.48f, 1f),
            ItemGrade.epic => new Color(0.84f, 0.50f, 1f),
            ItemGrade.legendary => new Color(1f, 0.63f, 0.12f),
            ItemGrade.unique => new Color(0.95f, 0.79f, 1f),
            _ => new Color(0.90f, 0.85f, 1f)
        };

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _rectTransform = GetComponent<RectTransform>();
            _background = background ? background : GetComponent<Image>();
            _glyph = glyphText;
            _runtimeName = nameText;
            _runtimeCount = countText;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                _onSelected?.Invoke(index);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_occupied) _onHover?.Invoke(index, area);
        }

        public void OnPointerExit(PointerEventData eventData) => _onHoverExit?.Invoke();

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!_occupied) return;
            _originPosition = _rectTransform.anchoredPosition;
            DragSlot = this;
            _canvasGroup.alpha = 0.72f;
            _canvasGroup.blocksRaycasts = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (DragSlot != this) return;
            var canvas = GetComponentInParent<Canvas>();
            float scaleFactor = canvas && canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
            _rectTransform.anchoredPosition += eventData.delta / scaleFactor;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (DragSlot != this) return;
            DragSlot = null;
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
            _rectTransform.anchoredPosition = _originPosition;
        }

        public void OnDrop(PointerEventData eventData)
        {
            var dragged = eventData.pointerDrag ? eventData.pointerDrag.GetComponent<ItemSlot>() : null;
            if (dragged == null || dragged == this || dragged._publisher == null) return;
            dragged._publisher.Publish(new InventorySwapMessage(dragged.index, index, dragged.area, area));
        }
    }
}
