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
using UnityEngine.InputSystem;
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
        [SerializeField] private Transform equipmentRoot;
        [SerializeField] private ItemDataBase itemDB;
        [SerializeField, HideInInspector] private int editorStyleVersion;
        [SerializeField, HideInInspector] private int editorLayoutVersion;

        [Header("Item details")]
        [SerializeField] private Text itemNameText;
        [SerializeField] private Text itemTypeText;
        [SerializeField] private Text itemDescriptionText;
        [SerializeField] private Text itemQuantityText;
        [SerializeField] private Text itemGlyphText;

        [Header("Hover tooltip")]
        [SerializeField] private RectTransform tooltipRoot;
        [SerializeField] private Text tooltipNameText;
        [SerializeField] private Text tooltipTypeText;
        [SerializeField] private Text tooltipDescriptionText;
        [SerializeField] private Text tooltipMetaText;

        [Header("Actions")]
        [SerializeField] private Button useButton;
        [SerializeField] private Button dropButton;
        [SerializeField] private Button sortButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button inventoryTabButton;
        [SerializeField] private Button craftTabButton;
        [SerializeField] private ModalNavigation modalNavigation;

        private readonly List<ItemSlot> _slotViews = new();
        private readonly List<ItemSlot> _equipmentSlotViews = new();
        private IDisposable _bag;
        private InventorySlotList _slotList;
        private IInventoryService _inventory;
        private IHotbarReader _hotbar;
        private IEquipmentReader _equipment;
        private IPublisher<UIReqMessage> _uiReqPublisher;
        private IPublisher<InvSwapMessage> _invSwapPublisher;
        private IHotbarInput _hotbarInput;
        private int _selectedIndex = -1;
        private InventorySlotArea _selectedArea = InventorySlotArea.Inventory;

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
                slots[i].ConfigureTooltip(ShowTooltip, HideTooltip);
                _slotViews.Add(slots[i]);
            }

            _equipmentSlotViews.Clear();
            if (!equipmentRoot) return;
            var equipmentSlots = equipmentRoot.GetComponentsInChildren<ItemSlot>(true);
            for (int i = 0; i < equipmentSlots.Length; i++)
            {
                int slotIndex = i;
                equipmentSlots[i].ConfigureSelection(_ => OnEquipmentSlotSelected(slotIndex));
                equipmentSlots[i].ConfigureTooltip(ShowTooltip, HideTooltip);
                _equipmentSlotViews.Add(equipmentSlots[i]);
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
            RefreshEquipment();
            SelectFirstOccupied();
        }

        public override void Close()
        {
            HideTooltip();
            base.Close();
        }

        public void OnInventoryTabClicked()
        {
            _uiReqPublisher?.Publish(new UIReqMessage(UIReqMsgType.Open, "Inventory"));
            _slotList?.RefreshAll();
            RefreshEquipment();
        }

        public void OnLogTabClicked()
        {
            _uiReqPublisher?.Publish(new UIReqMessage(UIReqMsgType.Open, "Log"));
        }

        public void OnCraftTabClicked() => OnLogTabClicked();

        private void OnInvMsg(InvChangedMessage msg)
        {
            HideTooltip();
            _slotList?.RefreshKeys(msg.changedKeys);
            if (msg.changedEquipmentKeys.Count > 0) RefreshEquipment();
            if (_selectedIndex >= 0) ShowDetails(_selectedArea, _selectedIndex);
        }

        private void OnSlotSelected(int index)
        {
            if (TryEquipFromInventory(index))
            {
                _selectedIndex = -1;
                _slotList?.SetSelected(-1);
                return;
            }
            _selectedIndex = index;
            _selectedArea = InventorySlotArea.Inventory;
            _slotList?.SetSelected(index);
            SetEquipmentSelected(-1);
            ShowDetails(_selectedArea, index);
        }

        private void OnEquipmentSlotSelected(int index)
        {
            if (_equipment == null || _invSwapPublisher == null || index < 0 ||
                index >= _equipment.EquipmentSlotCount) return;
            InventorySlot equipped = _equipment.GetEquipmentSlot(index);
            if (equipped == null || equipped.IsEmpty) return;
            int emptyIndex = FindFirstEmptyInventorySlot();
            if (emptyIndex < 0) return;
            _invSwapPublisher.Publish(new InvSwapMessage(index, emptyIndex,
                InventorySlotArea.Equipment, InventorySlotArea.Inventory));
            _selectedIndex = -1;
            SetEquipmentSelected(-1);
        }

        private bool TryEquipFromInventory(int inventoryIndex)
        {
            if (_inventory == null || _equipment == null || _invSwapPublisher == null || itemDB == null ||
                inventoryIndex < 0 || inventoryIndex >= _inventory.SlotCount) return false;
            InventorySlot slot = _inventory.GetSlot(inventoryIndex);
            if (slot == null || slot.IsEmpty) return false;
            Item item = itemDB.GetItem(slot.ins.itemId);
            if (!item.TryGetFeature<IEquipmentItem>(out var equipmentItem)) return false;
            int targetIndex = -1;
            for (int i = 0; i < _equipment.EquipmentSlotCount; i++)
            {
                if (_equipment.GetEquipmentSlotType(i) != equipmentItem.SlotType) continue;
                if (targetIndex < 0) targetIndex = i;
                InventorySlot equipped = _equipment.GetEquipmentSlot(i);
                if (equipped == null || equipped.IsEmpty)
                {
                    targetIndex = i;
                    break;
                }
            }
            if (targetIndex < 0) return false;
            _invSwapPublisher.Publish(new InvSwapMessage(inventoryIndex, targetIndex,
                InventorySlotArea.Inventory, InventorySlotArea.Equipment));
            return true;
        }

        private void ShowTooltip(int index, InventorySlotArea area)
        {
            if (!tooltipRoot || itemDB == null) return;
            InventorySlot slot = area == InventorySlotArea.Equipment
                ? _equipment?.GetEquipmentSlot(index)
                : _inventory?.GetSlot(index);
            if (slot == null || slot.IsEmpty)
            {
                HideTooltip();
                return;
            }
            Item item = itemDB.GetItem(slot.ins.itemId);
            SetText(tooltipNameText, string.IsNullOrWhiteSpace(item.itemName) ? $"아이템 {item.itemId}" : item.itemName);
            SetText(tooltipTypeText, $"{GetRoleName(item.Role)}  /  {item.itemGrade.ToString().ToUpperInvariant()}");
            SetText(tooltipDescriptionText, string.IsNullOrWhiteSpace(item.itemDesc)
                ? "설명이 등록되지 않은 아이템입니다."
                : item.itemDesc);
            SetText(tooltipMetaText, item.HasFeature<IEquipmentItem>()
                ? $"좌클릭: {(area == InventorySlotArea.Equipment ? "장비 해제" : "즉시 장착")}"
                : $"수량 {slot.stack}   ·   무게 {item.weight:0.##}");
            tooltipRoot.gameObject.SetActive(true);
            UpdateTooltipPosition();
        }

        private void HideTooltip()
        {
            if (tooltipRoot) tooltipRoot.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (tooltipRoot && tooltipRoot.gameObject.activeSelf) UpdateTooltipPosition();
        }

        private void UpdateTooltipPosition()
        {
            Vector2 pointer = Mouse.current?.position.ReadValue() ?? Vector2.zero;
            float width = tooltipRoot.rect.width;
            float height = tooltipRoot.rect.height;
            float x = Mathf.Clamp(pointer.x + 18f, 8f, Mathf.Max(8f, Screen.width - width - 8f));
            float y = Mathf.Clamp(pointer.y - 18f, height + 8f, Mathf.Max(height + 8f, Screen.height - 8f));
            tooltipRoot.position = new Vector3(x, y, 0f);
        }

        private void AssignSelectedToHotbar(int hotbarIndex)
        {
            if (!isViz || _selectedArea != InventorySlotArea.Inventory ||
                _inventory == null || _hotbar == null || _invSwapPublisher == null ||
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

        private void ShowDetails(int index) => ShowDetails(InventorySlotArea.Inventory, index);

        private void ShowDetails(InventorySlotArea area, int index)
        {
            if (_inventory == null || itemDB == null || index < 0)
            {
                ClearDetails();
                return;
            }

            InventorySlot slot;
            if (area == InventorySlotArea.Equipment)
            {
                if (_equipment == null || index >= _equipment.EquipmentSlotCount)
                {
                    ClearDetails();
                    return;
                }
                slot = _equipment.GetEquipmentSlot(index);
            }
            else
            {
                if (index >= _inventory.SlotCount)
                {
                    ClearDetails();
                    return;
                }
                slot = _inventory.GetSlot(index);
            }
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
            if (useButton) useButton.interactable = area == InventorySlotArea.Inventory && template.HasFeature<IUsable>();
            if (dropButton) dropButton.interactable = area == InventorySlotArea.Inventory;
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
            if (_selectedArea != InventorySlotArea.Inventory || _selectedIndex < 0 ||
                _inventory == null || !_inventory.UseItem(_selectedIndex)) return;
            _slotList?.RefreshAll();
            ShowDetails(_selectedIndex);
        }

        private void DropSelected()
        {
            if (_selectedArea != InventorySlotArea.Inventory || _selectedIndex < 0 ||
                _inventory == null || !_inventory.DropItem(_selectedIndex, 1)) return;
            _slotList?.RefreshAll();
            ShowDetails(_selectedIndex);
        }

        private void SortInventory()
        {
            if (_inventory == null) return;
            _inventory.SortItems();
            _selectedIndex = -1;
            _selectedArea = InventorySlotArea.Inventory;
            _slotList?.RefreshAll();
            _slotList?.SetSelected(-1);
            SelectFirstOccupied();
        }

        private void SelectFirstOccupied()
        {
            _selectedIndex = -1;
            _selectedArea = InventorySlotArea.Inventory;
            _slotList?.SetSelected(-1);
            SetEquipmentSelected(-1);
            ClearDetails();
        }

        private void RefreshEquipment()
        {
            if (_equipment == null || itemDB == null) return;
            int count = Mathf.Min(_equipmentSlotViews.Count, _equipment.EquipmentSlotCount);
            for (int i = 0; i < count; i++)
            {
                ItemSlot view = _equipmentSlotViews[i];
                view.Init(_invSwapPublisher, i, InventorySlotArea.Equipment);
                view.ConfigurePlaceholder(GetEquipmentGlyph(_equipment.GetEquipmentSlotType(i)),
                    GetEquipmentLabel(_equipment.GetEquipmentSlotType(i), i));
                InventorySlot slot = _equipment.GetEquipmentSlot(i);
                if (slot == null || slot.IsEmpty) view.Clear();
                else view.Set(slot, itemDB.GetItem(slot.ins.itemId), i);
            }
        }

        private void SetEquipmentSelected(int index)
        {
            for (int i = 0; i < _equipmentSlotViews.Count; i++)
                _equipmentSlotViews[i].SetSelected(i == index);
        }

        private static string GetEquipmentLabel(EquipmentSlotType type, int index) => type switch
        {
            EquipmentSlotType.Head => "머리",
            EquipmentSlotType.Body => "몸체",
            EquipmentSlotType.Legs => "다리",
            EquipmentSlotType.Feet => "발",
            EquipmentSlotType.UpgradeChip => $"강화 칩 {index - 3}",
            _ => "장비"
        };

        private static string GetEquipmentGlyph(EquipmentSlotType type) => type switch
        {
            EquipmentSlotType.Head => "◉",
            EquipmentSlotType.Body => "◇",
            EquipmentSlotType.Legs => "Ⅱ",
            EquipmentSlotType.Feet => "⌞",
            EquipmentSlotType.UpgradeChip => "⬡",
            _ => "＋"
        };

        private void CloseFromButton()
        {
            HideTooltip();
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
            IEquipmentReader equipment,
            ISubscriber<InvChangedMessage> invSubscriber,
            IPublisher<InvSwapMessage> invSwapPublisher,
            IPublisher<UIReqMessage> uiReqPublisher,
            IHotbarInput hotbarInput)
        {
            _bag?.Dispose();
            if (_hotbarInput != null) _hotbarInput.OnHotbarSlot -= AssignSelectedToHotbar;
            _inventory = invService;
            _hotbar = hotbar;
            _equipment = equipment;
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
            RefreshEquipment();
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
