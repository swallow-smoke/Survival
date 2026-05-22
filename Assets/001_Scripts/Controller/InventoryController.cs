using System;
using System.Collections.Generic;
using _001_Scripts.Data.Item;
using _001_Scripts.Data.Message;
using _001_Scripts.Data.SOJ;
using _001_Scripts.Interface;
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
        
        [SerializeField] private List<Item> items = new();
        [SerializeField] private ItemDataBase itemDB;

        public void AddItem(int id) => items.Add(itemDB.GetItem(id));
        public void AddItem(string name) => items.Add(itemDB.GetItem(name));
        public void AddItem(Item item) => items.Add(item);
        public void RemoveItem(int id) => items.Remove(itemDB.GetItem(id));
        public void RemoveItem(string name) => items.Remove(itemDB.GetItem(name));
        public void RemoveItem(Item item) => items.Remove(item);
        public bool HasItem(string name) => items.Contains(itemDB.GetItem(name));
        public bool HasItem(int id) => items.Contains(itemDB.GetItem(id));
        public bool HasItem(Item item) => items.Contains(item);
        
        /// <summary>
        /// Remove all the player item.
        /// So, plz use this method carefully.
        /// </summary>
        public void RemoveAll() => items.Clear();

        public void Start()
        {
            var bag = DisposableBag.CreateBuilder();
            invMessageSubScriber.Subscribe(message => OnMessageReceived(message)).AddTo(bag);

            _msgBag = bag.Build();
        }

        private void OnMessageReceived(InvMessage msg)
        {
            switch (msg.msgType)
            {
                case InvMessageType.Added:
                    AddItem(msg.item);
                    break;
                case InvMessageType.Removed:
                    RemoveItem(msg.item);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void OnDestroy() => _msgBag.Dispose();
    }
}