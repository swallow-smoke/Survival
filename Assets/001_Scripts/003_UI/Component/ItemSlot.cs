using System;
using _001_Scripts.Data.Item;
using _001_Scripts.Data.Message;
using _001_Scripts.Type.Item;
using MessagePipe;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _001_Scripts.UI.Component
{
    [RequireComponent(typeof(CanvasGroup))]
    public class ItemSlot : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
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

        private Image _background;
        private Text _glyph;
        private Text _runtimeName;
        private Text _runtimeCount;
        private CanvasGroup _canvasGroup;
        private IPublisher<InvSwapMessage> _publisher;
        private RectTransform _rectTransform;
        private Action<int> _onSelected;
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

        public void Init(IPublisher<InvSwapMessage> publisher, int slotIndex = -1,
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
                _glyph.color = GetItemColor(item.itemGrade);
            }
            if (_runtimeName)
                _runtimeName.text = string.IsNullOrWhiteSpace(item.itemName) ? $"아이템 {item.itemId}" : item.itemName;
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
                _glyph.text = "＋";
                _glyph.color = new Color(0.32f, 0.48f, 0.50f, 0.45f);
            }
            if (_runtimeName) _runtimeName.text = "";
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
                ? new Color(0.42f, 0.25f, 0.72f, 0.96f)
                : _occupied
                    ? new Color(0.13f, 0.08f, 0.23f, 0.88f)
                    : new Color(0.08f, 0.05f, 0.15f, 0.64f);
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
            dragged._publisher.Publish(new InvSwapMessage(dragged.index, index, dragged.area, area));
        }
    }
}
