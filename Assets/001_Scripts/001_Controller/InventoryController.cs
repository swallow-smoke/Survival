using System;
using System.Collections.Generic;
using System.Linq;
using _001_Scripts.Controller.Handler;
using _001_Scripts.Data.Item;
using _001_Scripts.Data.Message;
using _001_Scripts.Data.Message.Player;
using _001_Scripts.Data.SOJ;
using _001_Scripts.Interface;
using _001_Scripts.Type.States;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace _001_Scripts.Controller
{
    public class InventoryController : MonoBehaviour, IInventoryService, IHotbarReader, IHotbarActions
    {
        private IPublisher<InvChangedMessage> _invChangedPublisher;
        private IDisposable _messageBag;

        [SerializeField] private ItemDataBase itemDB;

        [Header("Inventory")]
        [SerializeField] private List<InventorySlot> items = new();
        [SerializeField] private int maxSlots = 40;
        [SerializeField, Range(1, 8)] private int hotbarSlotCount = 8;
        [SerializeField] private List<InventorySlot> hotbarItems = new();

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
                items[i] ??= EmptySlot();
            while (items.Count < maxSlots)
                items.Add(EmptySlot());

            hotbarSlotCount = Mathf.Clamp(hotbarSlotCount, 1, 8);
            hotbarItems ??= new List<InventorySlot>();
            if (hotbarItems.Count > hotbarSlotCount)
                hotbarItems.RemoveRange(hotbarSlotCount, hotbarItems.Count - hotbarSlotCount);
            for (int i = 0; i < hotbarItems.Count; i++)
                hotbarItems[i] ??= EmptySlot();
            while (hotbarItems.Count < hotbarSlotCount)
                hotbarItems.Add(EmptySlot());
        }

        private static InventorySlot EmptySlot() => new(null, 0);

        private void SwapItem(InvSwapMessage message)
        {
            NormalizeSlots();
            if (!IsValidAreaIndex(message.fromArea, message.fromIndex) ||
                !IsValidAreaIndex(message.toArea, message.toIndex)) return;

            var from = GetArea(message.fromArea);
            var to = GetArea(message.toArea);
            (from[message.fromIndex], to[message.toIndex]) =
                (to[message.toIndex], from[message.fromIndex]);

            var inventoryChanged = new List<int>();
            var hotbarChanged = new List<int>();
            AddChanged(message.fromArea, message.fromIndex, inventoryChanged, hotbarChanged);
            AddChanged(message.toArea, message.toIndex, inventoryChanged, hotbarChanged);
            PublishChanges(inventoryChanged, hotbarChanged);
        }

        private List<InventorySlot> GetArea(InventorySlotArea area) =>
            area == InventorySlotArea.Hotbar ? hotbarItems : items;

        private bool IsValidAreaIndex(InventorySlotArea area, int index)
        {
            var slots = GetArea(area);
            return index >= 0 && index < slots.Count;
        }

        private static void AddChanged(InventorySlotArea area, int index,
            List<int> inventoryChanged, List<int> hotbarChanged)
        {
            var target = area == InventorySlotArea.Hotbar ? hotbarChanged : inventoryChanged;
            if (!target.Contains(index)) target.Add(index);
        }

        public AddItemResult AddItem(int id, int count)
        {
            NormalizeSlots();
            if (count <= 0) return new AddItemResult(0, new List<int>());

            var template = itemDB.GetItem(id);
            int remaining = count;
            var changed = new List<int>();

            if (template.TryGetFeature<IStackable>(out var stackable))
            {
                int maximum = stackable.MaxStack;
                for (int i = 0; i < items.Count && remaining > 0; i++)
                {
                    var slot = items[i];
                    if (slot.IsEmpty || slot.ins.itemId != id || slot.stack >= maximum) continue;
                    int amount = Mathf.Min(remaining, maximum - slot.stack);
                    slot.stack += amount;
                    remaining -= amount;
                    changed.Add(i);
                }

                while (remaining > 0)
                {
                    int emptyIndex = FindEmptySlot();
                    if (emptyIndex < 0) break;
                    int amount = Mathf.Min(remaining, maximum);
                    items[emptyIndex] = new InventorySlot(itemDB.CreateInstance(id), amount);
                    remaining -= amount;
                    changed.Add(emptyIndex);
                }
            }
            else
            {
                while (remaining > 0)
                {
                    int emptyIndex = FindEmptySlot();
                    if (emptyIndex < 0) break;
                    items[emptyIndex] = new InventorySlot(itemDB.CreateInstance(id), 1);
                    remaining--;
                    changed.Add(emptyIndex);
                }
            }

            PublishChanges(changed);
            int added = count - remaining;
            if (added > 0) PublishItemNotification(id, added, NotificationKind.ItemAdded);
            return new AddItemResult(remaining, changed);
        }

        public void RemoveItem(int id, int count)
        {
            if (count <= 0) return;
            int remaining = count;
            var changed = new List<int>();
            var hotbarChanged = new List<int>();
            for (int i = items.Count - 1; i >= 0 && remaining > 0; i--)
            {
                var slot = items[i];
                if (slot.IsEmpty || slot.ins.itemId != id) continue;
                int amount = Mathf.Min(remaining, slot.stack);
                slot.stack -= amount;
                remaining -= amount;
                if (slot.stack <= 0) items[i] = EmptySlot();
                changed.Add(i);
            }
            for (int i = hotbarItems.Count - 1; i >= 0 && remaining > 0; i--)
            {
                var slot = hotbarItems[i];
                if (slot.IsEmpty || slot.ins.itemId != id) continue;
                int amount = Mathf.Min(remaining, slot.stack);
                slot.stack -= amount;
                remaining -= amount;
                if (slot.stack <= 0) hotbarItems[i] = EmptySlot();
                hotbarChanged.Add(i);
            }
            PublishChanges(changed, hotbarChanged);
            int removed = count - remaining;
            if (removed > 0) PublishItemNotification(id, removed, NotificationKind.ItemRemoved);
        }

        public void RemoveItem(Item item) => RemoveAll(slot => slot.ins.itemId == item.itemId);

        public void RemoveItem(Instance instance) => RemoveAll(slot => slot.ins.instanceId == instance.instanceId);

        public bool HasItem(int id, int count = 1) =>
            items.Concat(hotbarItems).Where(slot => slot != null && !slot.IsEmpty && slot.ins.itemId == id)
                .Sum(slot => slot.stack) >= count;

        public bool HasItem(Item item, int count = 1) => HasItem(item.itemId, count);

        public bool HasItem(Instance instance) =>
            items.Concat(hotbarItems)
                .Any(slot => slot != null && !slot.IsEmpty && slot.ins.instanceId == instance.instanceId);

        public IReadOnlyList<InventorySlot> GetAllItems()
        {
            NormalizeSlots();
            return items.AsReadOnly();
        }

        public InventorySlot GetSlot(int index)
        {
            NormalizeSlots();
            if (!IsValidIndex(index))
                throw new IndexOutOfRangeException($"Index {index} is out of range for inventory slots.");
            return items[index];
        }

        public InventorySlot GetHotbarSlot(int index)
        {
            if (index < 0 || index >= HotbarSlotCount)
                throw new IndexOutOfRangeException($"Hotbar index {index} is out of range.");
            return hotbarItems[index];
        }

        public bool SelectHotbar(int index)
        {
            if (_uiState != PlayerUIState.None || index < 0 || index >= HotbarSlotCount) return false;
            _selectedHotbarIndex = index;
            SyncHeldItem();
            _hotbarPublisher?.Publish(new HotbarSelectionMessage(index));
            return true;
        }

        public void CycleHotbar(int direction)
        {
            if (direction == 0 || HotbarSlotCount <= 0) return;
            int next = (_selectedHotbarIndex + (direction > 0 ? -1 : 1) + HotbarSlotCount) % HotbarSlotCount;
            SelectHotbar(next);
        }

        public bool UseItem(int index)
        {
            if (!IsValidIndex(index) || items[index].IsEmpty) return false;
            var item = itemDB.GetItem(items[index].ins.itemId);
            if (!item.TryGetFeature<IUsable>(out var usable)) return false;

            var player = GetComponent<PlayerController>();
            if (player == null) return false;
            usable.Use(player);
            RemoveAt(index, 1);
            return true;
        }

        public bool DropItem(int index, int count = 1)
        {
            if (!IsValidIndex(index) || items[index].IsEmpty || count <= 0) return false;
            RemoveAt(index, count);
            return true;
        }

        public void SortItems()
        {
            var occupied = items.Where(slot => slot != null && !slot.IsEmpty)
                .OrderBy(slot => itemDB.GetItem(slot.ins.itemId).itemType)
                .ThenBy(slot => itemDB.GetItem(slot.ins.itemId).itemName)
                .ThenBy(slot => slot.ins.itemId)
                .ToList();

            for (int i = 0; i < items.Count; i++)
                items[i] = i < occupied.Count ? occupied[i] : EmptySlot();
            PublishChanges(Enumerable.Range(0, items.Count).ToList());
        }

        private void RemoveAt(int index, int count)
        {
            var slot = items[index];
            slot.stack -= Mathf.Min(count, slot.stack);
            if (slot.stack <= 0) items[index] = EmptySlot();
            PublishChanges(index);
        }

        private void RemoveAll(Func<InventorySlot, bool> predicate)
        {
            var changed = new List<int>();
            var hotbarChanged = new List<int>();
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].IsEmpty || !predicate(items[i])) continue;
                items[i] = EmptySlot();
                changed.Add(i);
            }
            for (int i = 0; i < hotbarItems.Count; i++)
            {
                if (hotbarItems[i].IsEmpty || !predicate(hotbarItems[i])) continue;
                hotbarItems[i] = EmptySlot();
                hotbarChanged.Add(i);
            }
            PublishChanges(changed, hotbarChanged);
        }

        private int FindEmptySlot() => items.FindIndex(slot => slot == null || slot.IsEmpty);
        private bool IsValidIndex(int index)
        {
            NormalizeSlots();
            return index >= 0 && index < items.Count;
        }

        private void PublishChanges(params int[] indices) => PublishChanges(indices.ToList());

        private void PublishChanges(List<int> indices, List<int> hotbarIndices = null)
        {
            hotbarIndices ??= new List<int>();
            if (hotbarIndices.Contains(_selectedHotbarIndex)) SyncHeldItem();
            if (indices.Count > 0 || hotbarIndices.Count > 0)
                _invChangedPublisher?.Publish(new InvChangedMessage(indices, hotbarIndices));
        }

        private void SyncHeldItem()
        {
            if (!_itemHolder) _itemHolder = GetComponent<FirstPersonItemHolder>();
            if (!_itemHolder || _selectedHotbarIndex < 0 || _selectedHotbarIndex >= HotbarSlotCount) return;
            var slot = hotbarItems[_selectedHotbarIndex];
            if (slot == null || slot.IsEmpty)
            {
                _itemHolder.Unequip();
                return;
            }

            var item = itemDB.GetItem(slot.ins.itemId);
            if (!_itemHolder.TryEquip(item, slot.ins)) _itemHolder.Unequip();
        }

        private void PublishItemNotification(int itemId, int count, NotificationKind kind)
        {
            if (_notifications == null || itemDB == null) return;
            var item = itemDB.GetItem(itemId);
            string prefix = kind == NotificationKind.ItemAdded ? "+" : "-";
            _notifications.Show(new NotificationMessage(
                item.itemName,
                $"{prefix}{count}",
                _001_Scripts.UI.InventoryPanel.GetGlyph(item.itemType),
                kind));
        }

        private void OnMessageReceived(InvReqMessage message)
        {
            switch (message.msgType)
            {
                case InvMessageType.Added:
                    var result = AddItem(message.item, message.count);
                    int added = message.count - result.remain;
                    Debug.Log($"[Inventory] {itemDB.GetItem(message.item).itemName} +{added}" +
                              (result.remain > 0 ? $" ({result.remain} did not fit)" : ""));
                    break;
                case InvMessageType.Removed:
                    RemoveItem(message.item, message.count);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        [Inject]
        public void Construct(ISubscriber<InvReqMessage> invReqSubscriber,
            ISubscriber<InvSwapMessage> invSwapSubscriber,
            ISubscriber<PlayerUIStateMsg> uiStateSubscriber,
            IPublisher<InvChangedMessage> invChangedPublisher,
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
