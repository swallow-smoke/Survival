using System.Collections.Generic;
using _001_Scripts.Data;
using _001_Scripts.Data.Item;
using _001_Scripts.Data.SOJ;
using UnityEngine;

namespace _001_Scripts.Controller
{
    public class InventoryController : MonoBehaviour
    {
        [SerializeField] private List<Item> items = new();
        [SerializeField] private ItemDataBase itemDB;

        public void AddItem(int id) => items.Add(itemDB.GetItem(id));
        public void AddItem(string name) => items.Add(itemDB.GetItem(name));
        public void RemoveItem(int id) => items.Remove(itemDB.GetItem(id));
        public void RemoveItem(string name) => items.Remove(itemDB.GetItem(name));
        public bool HasItem(string name) => items.Contains(itemDB.GetItem(name));
        public bool HasItem(int id) => items.Contains(itemDB.GetItem(id));
    }
}