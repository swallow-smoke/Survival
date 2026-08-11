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
        
        private IPublisher<InvReqMessage> _invMessagePublisher;
        private IPublisher<CraftResultMessage> _craftResultMessagePublisher;
        
        private IInventoryReader _invServ;
        
        
        public void Craft(string itemName)
        {
            BluePrint result = bpDB.GetBluePrint(itemName);
            if (result == null)
            {
                Debug.LogWarning($"[Craft] Blueprint not found: {itemName}");
                return;
            }
            List<RecipeEntry> ingredient = result.recipe;
            int item = result.resultCraft;
            bool isUnlocked = result.isUnlocked;
            List<RecipeEntry> missingItems = new();
            
            if (!isUnlocked) return;
            
            ingredient.ForEach(e =>
            {
                if (!_invServ.HasItem(e.item, e.count))
                {
                    missingItems.Add(e);
                }
            });

            if (missingItems.Count > 0)
            {
                CraftResultMessage resultMsg = new CraftResultMessage(
                    CraftMessageType.Failed,
                    item,
                    missingItems);
                
                _craftResultMessagePublisher.Publish(resultMsg);
                return;
            };
            
            ingredient.ForEach(e =>
            {
                InvReqMessage resultMsg = new InvReqMessage(
                    InvMessageType.Removed,
                    e.item,
                    e.count);
                
                _invMessagePublisher.Publish(resultMsg);
            });
            


            InvReqMessage msg = new InvReqMessage(
                InvMessageType.Added,
                item, 
                1);
            _invMessagePublisher.Publish(msg);
            _craftResultMessagePublisher.Publish(new CraftResultMessage(CraftMessageType.Success, item));
        }

        private void OnMessageReceived(CraftReqMessage msg)
        {
            Craft(msg.itemName);
        }

        private void OnDestroy() => msgBag?.Dispose();

        [Inject]
        public void Constructor(IPublisher<InvReqMessage> invPublisher,
            IPublisher<CraftResultMessage> craftResultPublisher,
            ISubscriber<CraftReqMessage> craftMessageSubscriber,
            IInventoryReader invService)
        {
            msgBag?.Dispose();
            _invMessagePublisher = invPublisher;
            _craftResultMessagePublisher = craftResultPublisher;
            _invServ = invService;
            _craftMessageSubScriber = craftMessageSubscriber;

            var bag = DisposableBag.CreateBuilder();
            _craftMessageSubScriber.Subscribe(OnMessageReceived).AddTo(bag);
            msgBag = bag.Build();
        }
    }
}
