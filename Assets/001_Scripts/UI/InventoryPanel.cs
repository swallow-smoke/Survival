using System;
using System.Collections.Generic;
using _001_Scripts.Base;
using _001_Scripts.Data.Item;
using _001_Scripts.Data.Message;
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
        private List<ItemSlot> slots;

        [SerializeField] private int maxInvSlot;
        [SerializeField] private GameObject invSlotPrefab;
        [SerializeField] private Transform parentTrs;
        
        private void Awake()
        {
            slots = new List<ItemSlot>();
            for (int i = 0; i < maxInvSlot; i++)
            {
                var go = Instantiate(invSlotPrefab, parentTrs);
                slots.Add(go.GetComponent<ItemSlot>());
            }
        }

        private void RefreshInv()
        {
            foreach (var slot in slots)
                slot.Clear();
            
            var items = invService.GetAllItems();
            for (int i = 0; i < items.Count && i < slots.Count; i++)
                slots[i].Set(items[i]);
        }

        
        // make this later 
        // this function has architecture problem
        private void RefreshSlots(List<int> changedKeys, bool isStack)
        {
            
        }

        private void OnInvMsg(InvReqMessage msg)
        {
            RefreshInv();
        }

        public override void Open()
        {
            RefreshInv();
        }

        public override void Close()
        {
            
        }

        private void OnInvChanged(InvChangedMessage msg)
        {
            RefreshInv();
        }

        private void Subscribe()
        {
            var builder = DisposableBag.CreateBuilder();
            builder.Add(invSubscriber.Subscribe(OnInvChanged));
            bag = builder.Build();
        }

        [Inject]
        private void Construct(IInventoryService invService, ISubscriber<InvChangedMessage> invSubscriber)
        {
            this.invService = invService;
            this.invSubscriber = invSubscriber;
            
            Subscribe();
        }

        private void OnDestroy()
        {
            bag?.Dispose();
        }
    }
}