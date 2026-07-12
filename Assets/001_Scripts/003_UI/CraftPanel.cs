using System;
using System.Collections.Generic;
using _001_Scripts.Base;
using _001_Scripts.Data.Message;
using _001_Scripts.Data.SOJ;
using _001_Scripts.Interface;
using _001_Scripts.UI.Component;
using MessagePipe;
using TMPro;
using UnityEngine;
using VContainer;

namespace _001_Scripts.UI
{
    public class CraftPanel : PanelBase
    {
        [SerializeField] private BluePrintDataBase bpDB;
        [SerializeField] private ItemDataBase itemDB;
        [SerializeField] private GameObject blueprintSlotPrefab;
        [SerializeField] private Transform listParent;
        [SerializeField] private TMP_Text resultText;

        private readonly List<BlueprintSlot> _slots = new();

        private IDisposable _bag;
        private IInventoryService _inv;
        private IPublisher<CraftReqMessage> _craftReqPublisher;
        private IPublisher<UIReqMessage> _uiReqPublisher;

        [Inject]
        private void Construct(IPublisher<CraftReqMessage> craftReqPublisher,
            IPublisher<UIReqMessage> uiReqPublisher,
            ISubscriber<CraftResultMessage> craftResultSubscriber,
            ISubscriber<InvChangedMessage> invChangedSubscriber,
            IInventoryService inv)
        {
            _bag?.Dispose();

            _craftReqPublisher = craftReqPublisher;
            _uiReqPublisher = uiReqPublisher;
            _inv = inv;

            var builder = DisposableBag.CreateBuilder();
            builder.Add(craftResultSubscriber.Subscribe(OnCraftResult));
            builder.Add(invChangedSubscriber.Subscribe(OnInvChanged));
            _bag = builder.Build();

            BuildSlots();
        }

        private void BuildSlots()
        {
            var bluePrints = bpDB.GetAllBluePrints();
            for (int i = 0; i < bluePrints.Count; i++)
            {
                var bp = bluePrints[i];
                if (!bp.isUnlocked) continue;

                var go = Instantiate(blueprintSlotPrefab, listParent);
                var slot = go.GetComponent<BlueprintSlot>();
                slot.Init(bp, itemDB, OnCraftRequested);
                _slots.Add(slot);
            }
        }

        private void OnCraftRequested(string bluePrintName)
        {
            _craftReqPublisher.Publish(new CraftReqMessage(bluePrintName));
        }

        private void OnCraftResult(CraftResultMessage msg)
        {
            resultText.text = msg.msgType == CraftMessageType.Success
                ? $"제작 성공: {itemDB.GetItem(msg.itemId).itemName}"
                : "제작 실패";
        }

        private void OnInvChanged(InvChangedMessage msg)
        {
            RefreshAllAffordability();
        }

        private void RefreshAllAffordability()
        {
            for (int i = 0; i < _slots.Count; i++)
                _slots[i].RefreshAffordability(_inv);
        }

        public override void Open()
        {
            base.Open();
            RefreshAllAffordability();
        }

        public void OnCloseClicked()
        {
            _uiReqPublisher.Publish(new UIReqMessage(UIReqMsgType.Close, "Craft"));
        }

        private void OnDestroy()
        {
            _bag?.Dispose();
        }
    }
}
