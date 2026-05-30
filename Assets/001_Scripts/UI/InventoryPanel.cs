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
        private IInventoryService invService;
        [SerializeField] private int maxSlots = 40;
        private List<ItemSlot> slots;


        private void Awake()
        {
            slots.ForEach(obj =>
            {
                
            });
        }

        private void RefreshInv()
        {
            
        }

        private void OnInvMsg(InvMessage msg)
        {
            RefreshInv();
        }

        public void Open()
        {
            
        }

        public void Close()
        {
            
        }

        [Inject]
        private void Construct(IInventoryService invService, ISubscriber<InvMessage> invSubscriber)
        {
            var builder = DisposableBag.CreateBuilder();
            this.invService = invService;
            
            builder.Add(invSubscriber.Subscribe(OnInvMsg));
            
            bag = builder.Build();
        }
    }
}