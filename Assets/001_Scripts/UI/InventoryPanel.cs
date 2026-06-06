using System;
using System.Collections.Generic;
using _001_Scripts.Base;
using _001_Scripts.Data.Item;
using _001_Scripts.Data.Message;
using _001_Scripts.Data.SOJ;
using _001_Scripts.Interface;
using _001_Scripts.UI.Component;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace _001_Scripts.UI
{
    public class InventoryPanel : PanelBase
    {
        private IDisposable bag;
        private ISubscriber<InvChangedMessage> invSubscriber;
        private IInventoryService invService;
        private IPublisher<InvSwapMessage> invSwapPublisher;
        private List<ItemSlot> slots;

        [SerializeField] private int maxInvSlot;
        [SerializeField] private GameObject invSlotPrefab;
        [SerializeField] private Transform parentTrs;
        [SerializeField] private ItemDataBase itemDB;

        public override void Open()
        {
            base.Open();
            
            RefreshInv();
        }

        protected void Awake()
        {
            base.Awake();
        }

        private void RefreshInv()
        {
            foreach (var slot in slots)
                slot.Clear();

            var items = invService.GetAllItems();
            for (int i = 0; i < items.Count && i < slots.Count; i++)
                slots[i].Set(invService.GetSlot(i), itemDB.GetItem(invService.GetSlot(i).ins.itemId), i);
        }

        private void RefreshSlots(List<int> changedKeys)
        {
            changedKeys.ForEach(key =>
            {
                if (key >= slots.Count)
                    return;
                var slot = invService.GetSlot(key);
                if (slot.IsEmpty)
                    slots[key].Clear();
                else
                    slots[key].Set(slot, itemDB.GetItem(slot.ins.itemId), key);
            });
        }

        private void OnInvMsg(InvChangedMessage msg)
        {
            RefreshSlots(msg.changedKeys);
        }

        [Inject]
        private void Construct(IInventoryService invService, 
            ISubscriber<InvChangedMessage> invSubscriber,
            IPublisher<InvSwapMessage> invSwapPublisher)
        {
            bag?.Dispose();
            Debug.Log(GetInstanceID());
            this.invService = invService;
            this.invSubscriber = invSubscriber;
            this.invSwapPublisher = invSwapPublisher;

            var builder = DisposableBag.CreateBuilder();
            builder.Add(invSubscriber.Subscribe(OnInvMsg));
            bag = builder.Build();
            
            slots = new List<ItemSlot>();
            for (int i = 0; i < maxInvSlot; i++)
            {
                var go = Instantiate(invSlotPrefab, parentTrs);
                var slot = go.GetComponent<ItemSlot>();
                slot.Init(invSwapPublisher);
                slots.Add(slot);
            }
        }

        private void OnDestroy()
        {
            bag?.Dispose();
        }
    }
}