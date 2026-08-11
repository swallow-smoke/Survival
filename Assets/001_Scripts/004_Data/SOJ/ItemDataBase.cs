using System.Collections.Generic;
using _001_Scripts.Data.Item;
using AYellowpaper.SerializedCollections;
using UnityEngine;
using ItemDefinition = _001_Scripts.Data.Item.Item;

namespace _001_Scripts.Data.SOJ
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

        public Instance CreateInstance(int id)
        {
            var template = itemList[id];

            float durability = template.TryGetFeature<IEquippable>(out var equippable)
                ? equippable.MaxDurability
                : -1f;
            return new Instance(template.itemId, _nextIns++, durability);
        }
    }
}
