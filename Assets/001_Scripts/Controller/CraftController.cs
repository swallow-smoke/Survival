using System;
using System.Collections.Generic;
using _001_Scripts.Data.BluePrint;
using _001_Scripts.Data.Item;
using _001_Scripts.Data.Message;
using _001_Scripts.Data.SOJ;
using _001_Scripts.Interface;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace _001_Scripts.Controller
{
    public class CraftController : MonoBehaviour, ICraftService
    {
        [SerializeField] private BluePrintDataBase bpDB;
        private ISubscriber<CraftReqMessage> _craftMessageSubScriber;
        
        private IDisposable msgBag;
        
        private IPublisher<InvMessage> _invMessagePublisher;
        private IPublisher<CraftResultMessage> _craftResultMessagePublisher;
        
        private IInventoryService _invServ;
        
        
        public void Craft(string itemName)
        {
            BluePrint result = bpDB.GetBluePrint(itemName);
            List<Item> ingrediant = result.recipe;
            Item item = result.resultCraft;
            bool isUnlocked = result.isUnlocked;
            List<Item> missingItems = new();
            
            if (!isUnlocked) return;
            
            ingrediant.ForEach(item =>
            {
                if (!_invServ.HasItem(item))
                {
                    missingItems.Add(item);
                }
            });

            if (missingItems.Count > 0)
            {
                CraftResultMessage resultMsg = new CraftResultMessage(
                    CraftMessageType.Failed,
                    item.itemName,
                    missingItems);
                
                _craftResultMessagePublisher.Publish(resultMsg);
                return;
            };
            
            ingrediant.ForEach(item =>
            {
                InvMessage resultMsg = new InvMessage(
                    InvMessageType.Removed,
                    item);
                
                _invMessagePublisher.Publish(resultMsg);
            });
            


            InvMessage msg = new InvMessage(
                InvMessageType.Added,
                item);
            _invMessagePublisher.Publish(msg);
            _craftResultMessagePublisher.Publish(new CraftResultMessage(CraftMessageType.Success, item.itemName));
        }

        private void Start()
        {
            var bag = DisposableBag.CreateBuilder();
            _craftMessageSubScriber.Subscribe(msg => OnMessageReceived(msg)).AddTo(bag);
            
            msgBag = bag.Build();
        }

        private void OnMessageReceived(CraftReqMessage msg)
        {
            Craft(msg.itemName);
        }

        private void OnDestroy() => msgBag?.Dispose();

        [Inject]
        public void Constructor(IPublisher<InvMessage> invPublisher, IPublisher<CraftResultMessage> craftResultPublisher, IInventoryService invService)
        {
            _invMessagePublisher = invPublisher;
            _craftResultMessagePublisher = craftResultPublisher;
            _invServ = invService;
        }
    }
}