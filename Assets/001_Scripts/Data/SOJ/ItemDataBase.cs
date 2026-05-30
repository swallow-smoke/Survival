using System.Collections.Generic;
using UnityEngine;

namespace _001_Scripts.Data.SOJ
{
    [CreateAssetMenu(fileName = "ItemDataBase", menuName = "Data/Create ItemDB", order = 0)]
    public class ItemDataBase : ScriptableObject
    {
        [SerializeField] private List<Item.Item> itemList = new();
        private int _nextIns = 0;

        public Item.Item GetItem(int id)
        {
            Item.Item obj = itemList.Find(item => item.itemId == id);
            var clone = obj.Clone();
            clone.instanceId = _nextIns++;
            return clone ;
        }
        public Item.Item GetItem(string name)
        {
            Item.Item obj = itemList.Find(item => item.itemName == name);
            var clone = obj.Clone();
            clone.instanceId = _nextIns++;
            return clone ;
        }
        public Item.Item GetItem(Item.Item item)
        {
            Item.Item obj = itemList.Find(i => i == item);
            var clone = obj.Clone();
            clone.instanceId = _nextIns++;
            return clone ;
        }
        
        /// <summary>
        /// Made Only For Read
        /// Don't do change the item desc or info.
        /// It's can broken the Item DB System.
        /// </summary>
        /// <returns></returns>
        public IReadOnlyList<Item.Item> GetAllItems() => itemList;
        public bool Exist(int id) => itemList.Exists(item => item.itemId == id);
    }
}