using System;
using System.Collections.Generic;
using _001_Scripts.Base;
using _001_Scripts.Controller.Handler;
using _001_Scripts.Data.Item;
using _001_Scripts.Data.Message;
using _001_Scripts.Data.SOJ;
using _001_Scripts.Interface;
using _001_Scripts.Type.Item;
using _001_Scripts.UI.Component;
using MessagePipe;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _001_Scripts.UI
{
    [RequireComponent(typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))]
    [RequireComponent(typeof(CanvasGroup))]
    public class InventoryPanel : PanelBase
    {
        private const int VisibleSlotCount = 40;

        [Header("Data")]
        [SerializeField] private int maxInvSlot = VisibleSlotCount;
        [SerializeField] private GameObject invSlotPrefab;
        [SerializeField] private Transform parentTrs;
        [SerializeField] private ItemDataBase itemDB;
        [SerializeField, HideInInspector] private int editorStyleVersion;
        [SerializeField, HideInInspector] private int editorLayoutVersion;

        [Header("Item details")]
        [SerializeField] private Text itemNameText;
        [SerializeField] private Text itemTypeText;
        [SerializeField] private Text itemDescriptionText;
        [SerializeField] private Text itemQuantityText;
        [SerializeField] private Text itemGlyphText;

        [Header("Actions")]
        [SerializeField] private Button useButton;
        [SerializeField] private Button dropButton;
        [SerializeField] private Button sortButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button inventoryTabButton;
        [SerializeField] private Button craftTabButton;
        [SerializeField] private ModalNavigation modalNavigation;

        private readonly List<ItemSlot> _slotViews = new();
        private IDisposable _bag;
        private InventorySlotList _slotList;
        private IInventoryService _inventory;
        private IHotbarReader _hotbar;
        private IPublisher<UIReqMessage> _uiReqPublisher;
        private IPublisher<InvSwapMessage> _invSwapPublisher;
        private IHotbarInput _hotbarInput;
        private int _selectedIndex = -1;

        private new void Awake()
        {
            base.Awake();
            transform.localScale = Vector3.one;
            EnsureInteractionCanvas();
            SurvivalUITheme.ConfigureCanvas(gameObject, 1600, 900);
            foreach (var label in GetComponentsInChildren<Text>(true))
                label.font = SurvivalUITheme.Font;
            CollectSerializedSlots();
            BindButtons();
            ClearDetails();
        }

        private void EnsureInteractionCanvas()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.overrideSorting = true;
                canvas.sortingOrder = 120;
                canvas.enabled = true;
            }

            var raycaster = GetComponent<GraphicRaycaster>();
            if (!raycaster) raycaster = gameObject.AddComponent<GraphicRaycaster>();
            raycaster.enabled = true;
            raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;

            if (!GetComponent<CanvasScaler>()) gameObject.AddComponent<CanvasScaler>();
        }

        private void CollectSerializedSlots()
        {
            _slotViews.Clear();
            if (!parentTrs) return;

            var slots = parentTrs.GetComponentsInChildren<ItemSlot>(true);
            int count = Mathf.Min(slots.Length, Mathf.Min(VisibleSlotCount, Mathf.Max(1, maxInvSlot)));
            for (int i = 0; i < count; i++)
            {
                slots[i].ConfigureSelection(OnSlotSelected);
                _slotViews.Add(slots[i]);
            }
        }

        private void BindButtons()
        {
            if (useButton) useButton.onClick.AddListener(UseSelected);
            if (dropButton) dropButton.onClick.AddListener(DropSelected);
            if (sortButton) sortButton.onClick.AddListener(SortInventory);
            if (closeButton) closeButton.onClick.AddListener(CloseFromButton);
            if (inventoryTabButton) inventoryTabButton.onClick.AddListener(OnInventoryTabClicked);
            if (craftTabButton)
            {
                craftTabButton.onClick.AddListener(OnLogTabClicked);
                var label = craftTabButton.GetComponentInChildren<Text>(true);
                if (label) label.text = "로그   [V]";
            }
        }

        public override void Open()
        {
            base.Open();
            _slotList?.RefreshAll();
            SelectFirstOccupied();
        }

        public void OnInventoryTabClicked()
        {
            _uiReqPublisher?.Publish(new UIReqMessage(UIReqMsgType.Open, "Inventory"));
            _slotList?.RefreshAll();
        }

        public void OnLogTabClicked()
        {
            _uiReqPublisher?.Publish(new UIReqMessage(UIReqMsgType.Open, "Log"));
        }

        public void OnCraftTabClicked() => OnLogTabClicked();

        private void OnInvMsg(InvChangedMessage msg)
        {
            _slotList?.RefreshKeys(msg.changedKeys);
            if (_selectedIndex >= 0) ShowDetails(_selectedIndex);
        }

        private void OnSlotSelected(int index)
        {
            _selectedIndex = index;
            _slotList?.SetSelected(index);
            ShowDetails(index);
        }

        private void AssignSelectedToHotbar(int hotbarIndex)
        {
            if (!isViz || _inventory == null || _hotbar == null || _invSwapPublisher == null ||
                hotbarIndex < 0 || hotbarIndex >= _hotbar.HotbarSlotCount) return;

            int inventoryIndex = _selectedIndex;
            var hotbarSlot = _hotbar.GetHotbarSlot(hotbarIndex);
            bool hotbarOccupied = hotbarSlot != null && !hotbarSlot.IsEmpty;

            if (inventoryIndex < 0 || inventoryIndex >= _inventory.SlotCount)
            {
                if (!hotbarOccupied) return;
                inventoryIndex = FindFirstEmptyInventorySlot();
                if (inventoryIndex < 0) return;
                _selectedIndex = inventoryIndex;
                _slotList?.SetSelected(inventoryIndex);
            }

            var inventorySlot = _inventory.GetSlot(inventoryIndex);
            bool inventoryOccupied = inventorySlot != null && !inventorySlot.IsEmpty;
            if (!inventoryOccupied && !hotbarOccupied) return;

            _invSwapPublisher.Publish(new InvSwapMessage(inventoryIndex, hotbarIndex,
                InventorySlotArea.Inventory, InventorySlotArea.Hotbar));
        }

        private int FindFirstEmptyInventorySlot()
        {
            for (int i = 0; i < _inventory.SlotCount; i++)
            {
                var slot = _inventory.GetSlot(i);
                if (slot == null || slot.IsEmpty) return i;
            }

            return -1;
        }

        private void ShowDetails(int index)
        {
            if (_inventory == null || itemDB == null || index < 0 || index >= _inventory.SlotCount)
            {
                ClearDetails();
                return;
            }

            var slot = _inventory.GetSlot(index);
            if (slot == null || slot.IsEmpty)
            {
                ClearDetails();
                return;
            }

            var template = itemDB.GetItem(slot.ins.itemId);
            SetText(itemNameText, string.IsNullOrWhiteSpace(template.itemName) ? $"아이템 {template.itemId}" : template.itemName);
            SetText(itemTypeText, GetRoleName(template.Role) + "  /  " + template.itemGrade.ToString().ToUpperInvariant());
            SetText(itemQuantityText, $"보유 수량  {slot.stack}     무게  {template.weight:0.##}");
            SetText(itemDescriptionText, string.IsNullOrWhiteSpace(template.itemDesc)
                ? "수집한 생존 자원입니다. 제작과 탐사에 사용할 수 있습니다."
                : template.itemDesc);
            SetText(itemGlyphText, GetGlyph(template.itemType));
            if (useButton) useButton.interactable = template.HasFeature<IUsable>();
            if (dropButton) dropButton.interactable = true;
        }

        private void ClearDetails()
        {
            SetText(itemNameText, "아이템을 선택하세요");
            SetText(itemTypeText, "NO ITEM SELECTED");
            SetText(itemQuantityText, string.Empty);
            SetText(itemDescriptionText, "슬롯을 클릭하면 아이템 정보와 사용할 수 있는 기능이 표시됩니다.");
            SetText(itemGlyphText, "◇");
            if (useButton) useButton.interactable = false;
            if (dropButton) dropButton.interactable = false;
        }

        private void UseSelected()
        {
            if (_selectedIndex < 0 || _inventory == null || !_inventory.UseItem(_selectedIndex)) return;
            _slotList?.RefreshAll();
            ShowDetails(_selectedIndex);
        }

        private void DropSelected()
        {
            if (_selectedIndex < 0 || _inventory == null || !_inventory.DropItem(_selectedIndex, 1)) return;
            _slotList?.RefreshAll();
            ShowDetails(_selectedIndex);
        }

        private void SortInventory()
        {
            if (_inventory == null) return;
            _inventory.SortItems();
            _selectedIndex = -1;
            _slotList?.RefreshAll();
            _slotList?.SetSelected(-1);
            SelectFirstOccupied();
        }

        private void SelectFirstOccupied()
        {
            if (_inventory == null) return;
            for (int i = 0; i < Mathf.Min(_slotViews.Count, _inventory.SlotCount); i++)
            {
                var slot = _inventory.GetSlot(i);
                if (slot == null || slot.IsEmpty) continue;
                OnSlotSelected(i);
                return;
            }

            _selectedIndex = -1;
            _slotList?.SetSelected(-1);
            ClearDetails();
        }

        private void CloseFromButton()
        {
            if (_uiReqPublisher != null)
                _uiReqPublisher.Publish(new UIReqMessage(UIReqMsgType.Close, "Inventory"));
            else
                Close();
        }

        private static void SetText(Text target, string value)
        {
            if (target) target.text = value;
        }

        private static string GetTypeName(ItemType type) => type switch
        {
            ItemType.materials => "자원",
            ItemType.weapon => "무기",
            ItemType.armor => "보호 장비",
            ItemType.consumable => "소모품",
            _ => "아이템"
        };

        private static string GetRoleName(ItemRole role) => role switch
        {
            ItemRole.Tool => "도구",
            ItemRole.Usable => "사용 아이템",
            ItemRole.Equipment => "장비",
            ItemRole.Material => "재료",
            _ => "기타"
        };

        public static string GetGlyph(ItemType type) => type switch
        {
            ItemType.materials => "◆",
            ItemType.weapon => "⚔",
            ItemType.armor => "⬡",
            ItemType.consumable => "●",
            _ => "◇"
        };

        [Inject]
        private void Construct(IInventoryService invService,
            IHotbarReader hotbar,
            ISubscriber<InvChangedMessage> invSubscriber,
            IPublisher<InvSwapMessage> invSwapPublisher,
            IPublisher<UIReqMessage> uiReqPublisher,
            IHotbarInput hotbarInput)
        {
            _bag?.Dispose();
            if (_hotbarInput != null) _hotbarInput.OnHotbarSlot -= AssignSelectedToHotbar;
            _inventory = invService;
            _hotbar = hotbar;
            _uiReqPublisher = uiReqPublisher;
            modalNavigation?.Bind(uiReqPublisher);
            _invSwapPublisher = invSwapPublisher;
            _hotbarInput = hotbarInput;
            _hotbarInput.OnHotbarSlot += AssignSelectedToHotbar;

            var builder = DisposableBag.CreateBuilder();
            builder.Add(invSubscriber.Subscribe(OnInvMsg));
            _bag = builder.Build();

            if (_slotViews.Count == 0) CollectSerializedSlots();
            if (_slotViews.Count == 0)
            {
                Debug.LogError("[InventoryPanel] Serialized UGUI slots are missing. Run Tools/Survival UI/Rebuild Inventory Panel (UGUI).", this);
                return;
            }

            _slotList = new InventorySlotList(_slotViews, invSwapPublisher, invService, itemDB);
            _slotList.RefreshAll();
        }

        private void OnDestroy()
        {
            if (_hotbarInput != null) _hotbarInput.OnHotbarSlot -= AssignSelectedToHotbar;
            _bag?.Dispose();
            if (useButton) useButton.onClick.RemoveListener(UseSelected);
            if (dropButton) dropButton.onClick.RemoveListener(DropSelected);
            if (sortButton) sortButton.onClick.RemoveListener(SortInventory);
            if (closeButton) closeButton.onClick.RemoveListener(CloseFromButton);
            if (inventoryTabButton) inventoryTabButton.onClick.RemoveListener(OnInventoryTabClicked);
            if (craftTabButton) craftTabButton.onClick.RemoveListener(OnLogTabClicked);
        }
    }
}
