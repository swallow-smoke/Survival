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
        [Inject]
        private ISubscriber<InvMessage> invMessageSubScriber;
        private IDisposable _msgBag;
        
        [SerializeField] private ItemDataBase itemDB;

        private Dictionary<int, (Item template, int count)> stackableItems = new();
        [SerializeField] private List<Item> instanceItems = new();

        public void AddItem(int id, int count)
        {
            var item = itemDB.GetItem(id);
            if (item.HasAttributes(ItemAttributesType.Stackable))
            {
                int maxStock = (int)item.GetAttributeValue(ItemAttributesType.Stackable);

                var exisitingSlots = stackableItems.FirstOrDefault(e =>
                        e.Value.template.itemId == id && e.Value.count < maxStock);


                if (exisitingSlots.Value.template != null)
                {
                    int totalCount = exisitingSlots.Value.count + count;

                    if (totalCount <= maxStock)
                    {
                        stackableItems[exisitingSlots.Key] = (item, totalCount);
                    }
                    else
                    {
                        stackableItems[exisitingSlots.Key] = (item, maxStock);
                        int leftCount = totalCount - maxStock;
                        
                        AddItem(id, leftCount);
                    }
                }
                else
                {
                    int newKey = stackableItems.Count == 0? 0 : stackableItems.Keys.Max() + 1;
                    stackableItems[newKey] = (item, count);
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
                    instanceItems.Add(itemDB.GetItem(id));
                }
            }
        }

        /// <summary>
        /// only for instanced item
        /// </summary>
        /// <param name="item"></param>
        public void AddItem(Item item) => instanceItems.Add(item);

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
        
        
        
        // Legacy Inv management code
        // public void AddItem(string name) => items.Add(itemDB.GetItem(name));
        // public void AddItem(Item item) => items.Add(item);
        // public void RemoveItem(int id) => items.Remove(itemDB.GetItem(id));
        // public void RemoveItem(string name) => items.Remove(itemDB.GetItem(name));
        // public void RemoveItem(Item item) => items.Remove(item);
        // public bool HasItem(string name) => items.Contains(itemDB.GetItem(name));
        // public bool HasItem(int id) => items.Contains(itemDB.GetItem(id));
        // public bool HasItem(Item item) => items.Contains(item);

        public void Start()
        {
            var bag = DisposableBag.CreateBuilder();
            invMessageSubScriber.Subscribe(OnMessageReceived).AddTo(bag);

            _msgBag = bag.Build();
        }

        private void OnMessageReceived(InvMessage msg)
        {
            switch (msg.msgType)
            {
                case InvMessageType.Added:
                    if (msg.item.HasAttributes(ItemAttributesType.Stackable))
                        AddItem(msg.item.itemId, msg.count);
                    else AddItem(msg.item);
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

        private void OnDestroy() => _msgBag?.Dispose();
    }
}

