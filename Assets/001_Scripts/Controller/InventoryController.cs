using System;
using System.Collections.Generic;
using System.Linq;
using _001_Scripts.Data.Item;
using _001_Scripts.Data.Message;
using _001_Scripts.Data.SOJ;
using _001_Scripts.Interface;
using _001_Scripts.Type.Item;
using MessagePipe;
using NUnit.Framework;
using UnityEngine;
using VContainer;

namespace _001_Scripts.Controller
{
    public class InventoryController : MonoBehaviour, IInventoryService
    {
        private IPublisher<InvChangedMessage> invChangedPublisher;
        private IDisposable _msgBag;

        [SerializeField] private ItemDataBase itemDB;

        [Header("Inventory")] 
        [SerializeField] private List<Instance> Hotbar;
        [SerializeField] private List<InventorySlot> items = new();

        [SerializeField] private int maxSlots = 40;

        private void SwapItem(InvSwapMessage msg)
        {
            if (msg.fromIndex > items.Count || msg.toIndex > items.Count)
                return;
            
            (items[msg.fromIndex], items[msg.toIndex]) = (items[msg.toIndex], items[msg.fromIndex]);
            invChangedPublisher.Publish(new InvChangedMessage(new List<int> { msg.fromIndex, msg.toIndex }));
            
            Debug.Log($"Swapped Items {items[msg.fromIndex].stack} to {items[msg.toIndex].stack}");
        }

        public AddItemResult AddItem(int id, int count)
        {
            var template = itemDB.GetItem(id);
            int remain = count;
            List<int> changedKey = new();

            if (items.Count >= maxSlots)
                return new AddItemResult(remain, changedKey);

            if (template.HasAttribute(AttributesType.Stackable))
            {
                float maxStack = template.GetModifierValue(AttributesType.Stackable, ModifierType.MaxStack);
                var list = items.FindAll(e => !e.IsEmpty && e.ins.itemId == id);
                {
                    list.ForEach(slot =>
                    {
                        if (slot.stack >= maxStack)
                            return;
                        else
                        {
                            int oldStack = slot.stack;
                            slot.stack = Math.Min(slot.stack + remain, (int)maxStack);
                            remain = oldStack + remain > maxStack ? (int)(oldStack + remain - maxStack) : 0;
                            changedKey.Add(items.IndexOf(slot));
                        }
                    });

                    while (remain > 0 && items.Count < maxSlots)
                    {
                        var _itemIns = itemDB.CreateInstance(id);

                        int toAdd = Math.Min(remain, (int)maxStack);
                        items.Add(new InventorySlot(_itemIns, toAdd));
                        changedKey.Add(items.Count - 1);
                        remain -= toAdd;
                    }
                }
            }
            else
            {
                items.Add(new InventorySlot(itemDB.CreateInstance(id), 1));
                changedKey.Add(items.Count - 1);
                remain -= 1;
            }

            return new AddItemResult(remain, changedKey);
        }

        public void RemoveItem(int id, int count)
        {
            var template = itemDB.GetItem(id);
            int remain = count;

            if (template.HasAttribute(AttributesType.Stackable))
            {
                for (int i = items.Count - 1; i >= 0; i--)
                {
                    var e = items[i];

                    if (e.ins.itemId == id)
                    {
                        int removable = Mathf.Min(remain, e.stack);
                        e.stack -= removable;
                        remain -= removable;
                        if (e.stack <= 0)
                            items.RemoveAt(i);
                        if (remain == 0)
                            break;
                    }
                }
            }
            else
            {
                items.Remove(items.Find(e => e.ins.itemId == id));
            }
        }

        /// <summary>
        /// delete every items that same variant
        /// </summary>
        /// <param name="item"></param>
        public void RemoveItem(Template item)
        {
            items.RemoveAll(e => e.ins.itemId == item.itemId);
        }

        /// <summary>
        /// delete instance item
        /// </summary>
        /// <param name="ins"></param>
        public void RemoveItem(Instance ins)
        {
            items.RemoveAll(e => e.ins.instanceId == ins.instanceId);
        }

        public bool HasItem(int id, int count = 1)
        {
            var list = items.FindAll(e => !e.IsEmpty && e.ins.itemId == id);

            int total = list.Sum(e => e.stack);

            return total >= count;
        }

        public bool HasItem(Template template, int count = 1)
        {
            var list = items.FindAll(e => !e.IsEmpty && e.ins.itemId == template.itemId);

            int total = list.Sum(e => e.stack);

            return total >= count;
        }

        public bool HasItem(Instance ins)
        {
            return items.Exists(e => !e.IsEmpty && e.ins.instanceId == ins.instanceId);
        }

        public IReadOnlyList<InventorySlot> GetAllItems() => items.AsReadOnly();

        public InventorySlot GetSlot(int index)
        {
            if (index < 0 || index >= items.Count)
                throw new IndexOutOfRangeException($"Index {index} is out of range for inventory slots.");

            return items[index];
        }

        private void OnMessageReceived(InvReqMessage msg)
        {
            switch (msg.msgType)
            {
                case InvMessageType.Added: 
                    var result = AddItem(msg.item, msg.count);
                    invChangedPublisher.Publish(new InvChangedMessage(result.changeKeys));
                    break;
                case InvMessageType.Removed: 
                    RemoveItem(msg.item, msg.count);
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
            _msgBag?.Dispose();
            var bag = DisposableBag.CreateBuilder();
            this.invChangedPublisher = invChangedPublisher;

            invReqSubscriber.Subscribe(OnMessageReceived).AddTo(bag);
            invSwapSubscriber.Subscribe(SwapItem).AddTo(bag);

            _msgBag = bag.Build();
        }

        private void OnDestroy() => _msgBag?.Dispose();
    }
}