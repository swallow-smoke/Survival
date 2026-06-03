using System.Collections.Generic;
using _001_Scripts.Data.Item;
using _001_Scripts.Type.Item;
using AYellowpaper.SerializedCollections;
using UnityEngine;

namespace _001_Scripts.Data.SOJ
{
    [CreateAssetMenu(fileName = "ItemDataBase", menuName = "Data/Create ItemDB", order = 0)]
    public class ItemDataBase : ScriptableObject
    {
        [SerializedDictionary("index", "template")]
        public UnityEngine.Rendering.SerializedDictionary<int, Template> itemList = new();
        private int _nextIns = 0;

        /// <param name="id">index</param>
        /// <returns></returns>
        public Template GetItem(int id)
        {
            Template obj = itemList[id];
            return obj;
        }
        
        public IReadOnlyDictionary<int, Template> GetAllItems() => itemList;

        public Instance CreateInstance(int id)
        {
            var template = itemList[id];

            return new Instance(
                template.itemId,
                _nextIns++,
                template.GetModifierValue(
                    AttributesType.Equippable, 
                    ModifierType.DurabilityMax, 
                    -1f));
        }
    }
}