using System;
using _001_Scripts.Base;
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
        private InventorySlotList _slotList;

        [SerializeField] private int maxInvSlot;
        [SerializeField] private GameObject invSlotPrefab;
        [SerializeField] private Transform parentTrs;
        [SerializeField] private ItemDataBase itemDB;

        private IPublisher<UIReqMessage> _uiReqPublisher;

        public override void Open()
        {
            base.Open();
            _slotList.RefreshAll();
        }

        public void OnCraftTabClicked()
        {
            _uiReqPublisher.Publish(new UIReqMessage(UIReqMsgType.Close, "Inventory"));
            _uiReqPublisher.Publish(new UIReqMessage(UIReqMsgType.Open, "Craft"));
        }

        private void OnInvMsg(InvChangedMessage msg)
        {
            _slotList.RefreshKeys(msg.changedKeys);
        }

        [Inject]
        private void Construct(IInventoryService invService,
            ISubscriber<InvChangedMessage> invSubscriber,
            IPublisher<InvSwapMessage> invSwapPublisher,
            IPublisher<UIReqMessage> uiReqPublisher)
        {
            bag?.Dispose();

            _uiReqPublisher = uiReqPublisher;

            var builder = DisposableBag.CreateBuilder();
            builder.Add(invSubscriber.Subscribe(OnInvMsg));
            bag = builder.Build();

            _slotList = new InventorySlotList(maxInvSlot, invSlotPrefab, parentTrs, invSwapPublisher, invService, itemDB);
        }

        private void OnDestroy()
        {
            bag?.Dispose();
        }
    }
}
