using System;
using System.Collections.Generic;
using AstraNope.Data.Blueprints;
using AstraNope.Data.Items;
using AstraNope.Data.Messages;
using AstraNope.Data.Databases;
using AstraNope.Contracts;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace AstraNope.Gameplay.Player
{
    public class CraftController : MonoBehaviour, ICraftService
    {
        [SerializeField] private BluePrintDataBase bpDB;
        private ISubscriber<CraftReqMessage> _craftMessageSubScriber;
        
        private IDisposable msgBag;
        
        private IPublisher<CraftResultMessage> _craftResultMessagePublisher;
        
        private IInventoryReader _invServ;
        private IInventoryWriter _inventoryWriter;
        
        
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
            
            var requiredCounts = new Dictionary<int, int>();
            ingredient.ForEach(e =>
            {
                requiredCounts.TryGetValue(e.item, out int current);
                requiredCounts[e.item] = current + e.count;
            });

            foreach (var required in requiredCounts)
            {
                if (!_invServ.HasItem(required.Key, required.Value))
                {
                    missingItems.Add(new RecipeEntry { item = required.Key, count = required.Value });
                }
            }

            if (missingItems.Count > 0)
            {
                CraftResultMessage resultMsg = new CraftResultMessage(
                    CraftMessageType.Failed,
                    item,
                    missingItems);
                
                _craftResultMessagePublisher.Publish(resultMsg);
                return;
            };
            
            foreach (var required in requiredCounts)
                _inventoryWriter.RemoveItem(required.Key, required.Value);

            _inventoryWriter.AddItem(item, 1);
            _craftResultMessagePublisher.Publish(new CraftResultMessage(CraftMessageType.Success, item));
        }

        private void OnMessageReceived(CraftReqMessage msg)
        {
            Craft(msg.itemName);
        }

        private void OnDestroy() => msgBag?.Dispose();

        [Inject]
        public void Constructor(IInventoryWriter inventoryWriter,
            IPublisher<CraftResultMessage> craftResultPublisher,
            ISubscriber<CraftReqMessage> craftMessageSubscriber,
            IInventoryReader invService)
        {
            msgBag?.Dispose();
            _inventoryWriter = inventoryWriter;
            _craftResultMessagePublisher = craftResultPublisher;
            _invServ = invService;
            _craftMessageSubScriber = craftMessageSubscriber;

            var bag = DisposableBag.CreateBuilder();
            _craftMessageSubScriber.Subscribe(OnMessageReceived).AddTo(bag);
            msgBag = bag.Build();
        }
    }
}
