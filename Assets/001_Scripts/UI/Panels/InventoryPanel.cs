using System;
using System.Collections.Generic;
using AstraNope.UI.Base;
using AstraNope.Data.Items;
using AstraNope.Data.Messages;
using AstraNope.Data.Databases;
using AstraNope.Contracts;
using AstraNope.Data.Items.Types;
using AstraNope.UI.Components;
using MessagePipe;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VContainer;

using AstraNope.Localization;
namespace AstraNope.UI.Panels
{
    [RequireComponent(typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))]
    [RequireComponent(typeof(CanvasGroup))]
    public partial class InventoryPanel : PanelBase
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
        private IPublisher<InventorySwapMessage> _invSwapPublisher;
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
                if (label) label.text = L10n.T("k_f96235442f");
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

        private void OnInvMsg(InventoryChangedMessage msg)
        {
            HideTooltip();
            _slotList?.RefreshKeys(msg.changedKeys);
            if (msg.changedEquipmentKeys.Count > 0) RefreshEquipment();
            if (_selectedIndex >= 0) ShowDetails(_selectedArea, _selectedIndex);
        }
        private void Update()
        {
            if (tooltipRoot && tooltipRoot.gameObject.activeSelf) UpdateTooltipPosition();
        }
        private void CloseFromButton()
        {
            HideTooltip();
            if (_uiReqPublisher != null)
                _uiReqPublisher.Publish(new UIReqMessage(UIReqMsgType.Close, "Inventory"));
            else
                Close();
        }
        [Inject]
        private void Construct(IInventoryService invService,
            IHotbarReader hotbar,
            IEquipmentReader equipment,
            ISubscriber<InventoryChangedMessage> invSubscriber,
            IPublisher<InventorySwapMessage> invSwapPublisher,
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