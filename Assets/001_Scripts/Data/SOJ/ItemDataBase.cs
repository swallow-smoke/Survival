using System.Collections.Generic;
using UnityEngine;

namespace _001_Scripts.Data.SOJ
{
    [CreateAssetMenu(fileName = "ItemDataBase", menuName = "Data/Create ItemDB", order = 0)]
    public class ItemDataBase : ScriptableObject
    {
        [SerializeField] private List<Item.Item> itemList = new();

        public Item.Item GetItem(int id)
        {
            Item.Item obj = itemList.Find(item => item.itemId == id);
            return obj.Clone();
        }
        public Item.Item GetItem(string name)
        {
            Item.Item obj = itemList.Find(item => item.itemName == name);
            return obj.Clone();
        }
        public Item.Item GetItem(Item.Item item)
        {
            Item.Item obj = itemList.Find(i => i == item);
            return obj.Clone();
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