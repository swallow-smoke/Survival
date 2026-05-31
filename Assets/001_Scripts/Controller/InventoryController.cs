using System;
using System.Collections.Generic;
using System.Linq;
using _001_Scripts.Data.Item;
using _001_Scripts.Data.Message;
using _001_Scripts.Data.SOJ;
using _001_Scripts.Interface;
using _001_Scripts.Type.Item;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace _001_Scripts.Controller
{
    public class InventoryController : MonoBehaviour, IInventoryService
    {
        private IPublisher<InvChangedMessage> invChangedPublisher;
        private IDisposable _msgBag;

        [SerializeField] private ItemDataBase itemDB;

        private Dictionary<int, (Item template, int count)> stackableItems = new();
        [SerializeField] private List<Item> instanceItems = new();

        [SerializeField] private int maxSlots = 40;

        public AddItemResult AddItem(int id, int count)
        {
            var item = itemDB.GetItem(id);
            int remain = count;
            List<int> changedKey = new();

            while (remain > 0)
            {
                if (item.HasAttributes(ItemAttributesType.Stackable))
                {
                    int maxStock = (int)item.GetAttributeValue(ItemAttributesType.Stackable);

                    var exisitingSlots = stackableItems.FirstOrDefault(e =>
                        e.Value.template.itemId == id && e.Value.count < maxStock);


                    if (exisitingSlots.Value.template != null)
                    {
                        int totalCount = exisitingSlots.Value.count + remain;

                        if (totalCount <= maxStock)
                        {
                            stackableItems[exisitingSlots.Key] = (item, totalCount);
                            changedKey.Add(exisitingSlots.Key);
                            remain = 0;
                        }
                        else
                        {
                            stackableItems[exisitingSlots.Key] = (item, maxStock);
                            changedKey.Add(exisitingSlots.Key);
                            int leftCount = totalCount - maxStock;

                            remain = leftCount;
                        }
                    }
                    else
                    {
                        if (stackableItems.Count >= maxSlots)
                            break;

                        int newKey = stackableItems.Count == 0 ? 0 : stackableItems.Keys.Max() + 1;

                        int stock = Mathf.Min(remain, maxStock);

                        stackableItems[newKey] = (item, stock);
                        changedKey.Add(newKey);

                        remain -= stock;
                    }

                    // if (stackableItems.ContainsKey(item.itemId))
                    // {
                    //     var existing = stackableItems[item.itemId];
                    //     if (existing.count > item.GetAttributes(ItemAttributesType.Stackable).value)
                    //     {
                    //         stackableItems.Add(stackableItems.Count + 1, (existing.template, count));
                    //     }
                    //     else 
                    //         stackableItems[item.itemId] = (existing.template, existing.count + count);
                    // }
                    // else
                    // {
                    //     stackableItems.Add(stackableItems.Count + 1, (item, count));
                    // }
                }
                else
                {
                    for (int i = 0; i < count; i++)
                    {
                        if (instanceItems.Count >= maxSlots)
                            break;

                        instanceItems.Add(itemDB.GetItem(id));
                        remain -= 1;
                    }
                }
            }

            return new AddItemResult(remain, changedKey);
        }

        /// <summary>
        /// only for instanced item
        /// </summary>
        /// <param name="item"></param>
        public AddItemResult AddItem(Item item)
        {
            if (instanceItems.Count >= maxSlots)
                return new AddItemResult(1, new List<int>());

            instanceItems.Add(item);
            return new AddItemResult(0, new List<int>());
        }

        public void RemoveItem(int id, int count)
        {
            var targetItem = itemDB.GetItem(id);

            if (targetItem.HasAttributes(ItemAttributesType.Stackable))
            {
                var exisitingSlots = stackableItems.FirstOrDefault(e =>
                    e.Value.template.itemId == id);

                if (exisitingSlots.Value.template == null) return;

                if (exisitingSlots.Value.count >= count)
                {
                    int remain = exisitingSlots.Value.count - count;

                    if (remain == 0)
                    {
                        stackableItems.Remove(exisitingSlots.Key);
                    }
                    else
                    {
                        stackableItems[exisitingSlots.Key] = (targetItem, remain);
                    }
                }
                else
                {
                    int leftCount = count - exisitingSlots.Value.count;
                    stackableItems.Remove(exisitingSlots.Key);
                    RemoveItem(id, leftCount);
                }
            }
        }

        public void RemoveItem(Item item) => instanceItems.RemoveAll(e => e.instanceId == item.instanceId);

        public bool HasItem(int id, int count)
        {
            var targetItem = itemDB.GetItem(id);

            if (targetItem.HasAttributes(ItemAttributesType.Stackable))
            {
                int totalCount = stackableItems
                    .Where(e => e.Value.template.itemId == id)
                    .Sum(e => e.Value.count);

                return totalCount >= count;
            }
            else
            {
                return instanceItems.Any(e => e.itemId == id);
            }
        }

        public bool HasItem(Item item) => instanceItems.Exists(e => e.instanceId == item.instanceId);

        public IReadOnlyList<InventorySlotData> GetAllItems()
        {
            List<InventorySlotData> result = new List<InventorySlotData>();

            foreach (var slot in stackableItems.Values)
                result.Add(new InventorySlotData(slot.template, slot.count));

            foreach (var item in instanceItems)
                result.Add(new InventorySlotData(item, 1));

            return result;
        }

        public int GetSlot(int index) => stackableItems.ContainsKey(index) ? stackableItems[index].count : 0;


        // Legacy Inv management code
        // public void AddItem(string name) => items.Add(itemDB.GetItem(name));
        // public void AddItem(Item item) => items.Add(item);
        // public void RemoveItem(int id) => items.Remove(itemDB.GetItem(id));
        // public void RemoveItem(string name) => items.Remove(itemDB.GetItem(name));
        // public void RemoveItem(Item item) => items.Remove(item);
        // public bool HasItem(string name) => items.Contains(itemDB.GetItem(name));
        // public bool HasItem(int id) => items.Contains(itemDB.GetItem(id));
        // public bool HasItem(Item item) => items.Contains(item);

        private void OnMessageReceived(InvReqMessage msg)
        {
            switch (msg.msgType)
            {
                case InvMessageType.Added:
                    if (msg.item.HasAttributes(ItemAttributesType.Stackable))
                    {
                        var result = AddItem(msg.item.itemId, msg.count);

                        if (result.remain > 0)
                            Debug.Log($"Drop: {msg.item.itemName} x{result.remain}");
                        invChangedPublisher.Publish(
                            new InvChangedMessage(
                                result.changeKeys,
                                true)
                        );
                    }
                    else
                    {
                        var result = AddItem(msg.item);

                        if (result.remain > 0)
                            Debug.Log($"Drop: {msg.item.itemName} x{result.remain}");

                        invChangedPublisher.Publish(
                            new InvChangedMessage(
                                result.changeKeys,
                                false)
                        );
                    }

                    break;
                case InvMessageType.Removed:
                    if (msg.item.HasAttributes(ItemAttributesType.Stackable))
                        RemoveItem(msg.item.itemId, msg.count);
                    else
                        RemoveItem(msg.item);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        [Inject]
        public void Construct(ISubscriber<InvReqMessage> invReqSubscriber,
            IPublisher<InvChangedMessage> invChangedPublisher)
        {
            var bag = DisposableBag.CreateBuilder();
            this.invChangedPublisher = invChangedPublisher;

            invReqSubscriber.Subscribe(OnMessageReceived).AddTo(bag);

            _msgBag = bag.Build();
        }

        private void OnDestroy() => _msgBag?.Dispose();
    }
}