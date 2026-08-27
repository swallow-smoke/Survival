using System;
using System.Collections.Generic;
using System.Linq;
using AstraNope.Gameplay.Input;
using AstraNope.Data.Items;
using AstraNope.Data.Messages;
using AstraNope.Data.Messages.Player;
using AstraNope.Data.Databases;
using AstraNope.Contracts;
using AstraNope.Types.States;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace AstraNope.Gameplay.Player
{
    [DisallowMultipleComponent]
    public partial class InventoryController : MonoBehaviour, IInventoryService, IHotbarReader, IHotbarActions, IEquipmentReader,
        IItemCatalog
    {
        private const int EquipmentSlots = 8;
        private IPublisher<InventoryChangedMessage> _invChangedPublisher;
        private IDisposable _messageBag;

        [SerializeField] private ItemDataBase itemDB;

        [Header("Inventory")]
        [SerializeField] private List<InventorySlot> items = new();
        [SerializeField] private int maxSlots = 40;
        [SerializeField, Range(1, 8)] private int hotbarSlotCount = 8;
        [SerializeField] private List<InventorySlot> hotbarItems = new();

        [Header("Player Equipment")]
        [SerializeField] private List<InventorySlot> equipmentItems = new();

        private FirstPersonItemHolder _itemHolder;
        private IInputService _input;
        private IPublisher<HotbarSelectionMessage> _hotbarPublisher;
        private INotificationService _notifications;
        private PlayerUIState _uiState;
        private int _selectedHotbarIndex;

        public int HotbarSlotCount
        {
            get
            {
                NormalizeSlots();
                return hotbarItems.Count;
            }
        }
        public int SelectedHotbarIndex => _selectedHotbarIndex;
        public int EquipmentSlotCount => EquipmentSlots;

        public bool TryGetItem(int id, out Item item)
        {
            item = null;
            return itemDB && itemDB.itemList.TryGetValue(id, out item) && item != null;
        }

        public int SlotCount
        {
            get
            {
                NormalizeSlots();
                return items.Count;
            }
        }

        private void Awake()
        {
            NormalizeSlots();
            _itemHolder = GetComponent<FirstPersonItemHolder>();
        }

        private void Start() => SyncHeldItem();

        private void NormalizeSlots()
        {
            maxSlots = Mathf.Max(1, maxSlots);
            items ??= new List<InventorySlot>();
            if (items.Count > maxSlots)
                items.RemoveRange(maxSlots, items.Count - maxSlots);
            for (int i = 0; i < items.Count; i++)
                if (items[i] == null || items[i].ins == null || items[i].stack <= 0)
                    items[i] = EmptySlot();
            while (items.Count < maxSlots)
                items.Add(EmptySlot());

            hotbarSlotCount = Mathf.Clamp(hotbarSlotCount, 1, 8);
            hotbarItems ??= new List<InventorySlot>();
            if (hotbarItems.Count > hotbarSlotCount)
                hotbarItems.RemoveRange(hotbarSlotCount, hotbarItems.Count - hotbarSlotCount);
            for (int i = 0; i < hotbarItems.Count; i++)
                if (hotbarItems[i] == null || hotbarItems[i].ins == null || hotbarItems[i].stack <= 0)
                    hotbarItems[i] = EmptySlot();
            while (hotbarItems.Count < hotbarSlotCount)
                hotbarItems.Add(EmptySlot());

            equipmentItems ??= new List<InventorySlot>();
            if (equipmentItems.Count > EquipmentSlots)
                equipmentItems.RemoveRange(EquipmentSlots, equipmentItems.Count - EquipmentSlots);
            for (int i = 0; i < equipmentItems.Count; i++)
                if (equipmentItems[i] == null || equipmentItems[i].ins == null || equipmentItems[i].stack <= 0)
                    equipmentItems[i] = EmptySlot();
            while (equipmentItems.Count < EquipmentSlots)
                equipmentItems.Add(EmptySlot());
        }

        private static InventorySlot EmptySlot() => new(null, 0);
        [Inject]
        public void Construct(ISubscriber<InventoryRequestMessage> invReqSubscriber,
            ISubscriber<InventorySwapMessage> invSwapSubscriber,
            ISubscriber<PlayerUIStateMessage> uiStateSubscriber,
            IPublisher<InventoryChangedMessage> invChangedPublisher,
            IPublisher<HotbarSelectionMessage> hotbarPublisher,
            INotificationService notifications,
            IInputService inputService)
        {
            _messageBag?.Dispose();
            if (_input != null)
            {
                _input.OnHotbarSlot -= HandleHotbarSlot;
                _input.OnHotbarScroll -= HandleHotbarScroll;
            }
            _invChangedPublisher = invChangedPublisher;
            _hotbarPublisher = hotbarPublisher;
            _notifications = notifications;
            _input = inputService;
            var bag = DisposableBag.CreateBuilder();
            invReqSubscriber.Subscribe(OnMessageReceived).AddTo(bag);
            invSwapSubscriber.Subscribe(SwapItem).AddTo(bag);
            uiStateSubscriber.Subscribe(message => _uiState = message.state).AddTo(bag);
            _messageBag = bag.Build();
            _input.OnHotbarSlot += HandleHotbarSlot;
            _input.OnHotbarScroll += HandleHotbarScroll;
        }

        private void HandleHotbarSlot(int index) => SelectHotbar(index);
        private void HandleHotbarScroll(float direction) => CycleHotbar(direction > 0f ? 1 : -1);

        private void OnDestroy()
        {
            if (_input != null)
            {
                _input.OnHotbarSlot -= HandleHotbarSlot;
                _input.OnHotbarScroll -= HandleHotbarScroll;
            }
            _messageBag?.Dispose();
        }
    }
}