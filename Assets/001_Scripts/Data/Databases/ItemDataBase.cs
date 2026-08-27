using System.Collections.Generic;
using AstraNope.Data.Items;
using AYellowpaper.SerializedCollections;
using UnityEngine;
using ItemDefinition = AstraNope.Data.Items.Item;

namespace AstraNope.Data.Databases
{
    [CreateAssetMenu(fileName = "ItemDataBase", menuName = "Data/Create ItemDB", order = 0)]
    public class ItemDataBase : ScriptableObject
    {
        [SerializedDictionary("index", "item")]
        public SerializedDictionary<int, ItemDefinition> itemList = new();
        private int _nextIns = 0;

        /// <param name="id">index</param>
        /// <returns></returns>
        public ItemDefinition GetItem(int id)
        {
            ItemDefinition obj = itemList[id];
            return obj;
        }
        
        public IReadOnlyDictionary<int, ItemDefinition> GetAllItems() => itemList;

        public ItemInstance CreateInstance(int id)
        {
            var template = itemList[id];

            float durability = template.TryGetFeature<IEquippable>(out var equippable)
                ? equippable.MaxDurability
                : -1f;
            return new ItemInstance(template.itemId, _nextIns++, durability);
        }
    }
}
