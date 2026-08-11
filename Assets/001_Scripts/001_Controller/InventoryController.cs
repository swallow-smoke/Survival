using System;
using System.Collections.Generic;
using System.Linq;
using _001_Scripts.Data.Item;
using _001_Scripts.Data.Message;
using _001_Scripts.Data.SOJ;
using _001_Scripts.Interface;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace _001_Scripts.Controller
{
    public class InventoryController : MonoBehaviour, IInventoryService
    {
        private IPublisher<InvChangedMessage> _invChangedPublisher;
        private IDisposable _messageBag;

        [SerializeField] private ItemDataBase itemDB;

        [Header("Inventory")]
        [SerializeField] private List<Instance> Hotbar;
        [SerializeField] private List<InventorySlot> items = new();
        [SerializeField] private int maxSlots = 40;

        public int SlotCount
        {
            get
            {
                NormalizeSlots();
                return items.Count;
            }
        }

        private void Awake() => NormalizeSlots();

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
        }

        private static InventorySlot EmptySlot() => new(null, 0);

        private void SwapItem(InvSwapMessage message)
        {
            if (!IsValidIndex(message.fromIndex) || !IsValidIndex(message.toIndex)) return;
            (items[message.fromIndex], items[message.toIndex]) = (items[message.toIndex], items[message.fromIndex]);
            PublishChanges(message.fromIndex, message.toIndex);
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

            return new AddItemResult(remaining, changed);
        }

        public void RemoveItem(int id, int count)
        {
            if (count <= 0) return;
            int remaining = count;
            var changed = new List<int>();
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
            PublishChanges(changed);
        }

        public void RemoveItem(Item item) => RemoveAll(slot => slot.ins.itemId == item.itemId);

        public void RemoveItem(Instance instance) => RemoveAll(slot => slot.ins.instanceId == instance.instanceId);

        public bool HasItem(int id, int count = 1) =>
            items.Where(slot => slot != null && !slot.IsEmpty && slot.ins.itemId == id).Sum(slot => slot.stack) >= count;

        public bool HasItem(Item item, int count = 1) => HasItem(item.itemId, count);

        public bool HasItem(Instance instance) =>
            items.Any(slot => slot != null && !slot.IsEmpty && slot.ins.instanceId == instance.instanceId);

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
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].IsEmpty || !predicate(items[i])) continue;
                items[i] = EmptySlot();
                changed.Add(i);
            }
            PublishChanges(changed);
        }

        private int FindEmptySlot() => items.FindIndex(slot => slot == null || slot.IsEmpty);
        private bool IsValidIndex(int index)
        {
            NormalizeSlots();
            return index >= 0 && index < items.Count;
        }

        private void PublishChanges(params int[] indices) => PublishChanges(indices.ToList());

        private void PublishChanges(List<int> indices)
        {
            if (indices.Count > 0)
                _invChangedPublisher?.Publish(new InvChangedMessage(indices));
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
                    PublishChanges(result.changeKeys);
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
            IPublisher<InvChangedMessage> invChangedPublisher)
        {
            _messageBag?.Dispose();
            _invChangedPublisher = invChangedPublisher;
            var bag = DisposableBag.CreateBuilder();
            invReqSubscriber.Subscribe(OnMessageReceived).AddTo(bag);
            invSwapSubscriber.Subscribe(SwapItem).AddTo(bag);
            _messageBag = bag.Build();
        }

        private void OnDestroy() => _messageBag?.Dispose();
    }
}
