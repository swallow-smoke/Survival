using System.Collections.Generic;
using _001_Scripts.Type.Item;
using UnityEngine;

namespace _001_Scripts.Data.Item
{
    [System.Serializable]
    public class Item
    {
        [Header("Identifier")]
        public int itemId;
        public int instanceId;
        
        [Header("Item Info")]
        public string itemName;
        public string itemDesc;
        public float durability;
        public ItemGrade itemGrade;
        public ItemType itemType;
        public List<ItemAttributes> ItemAttributes;

        public Item Clone()
        {
            Item cloneItem = (Item)this.MemberwiseClone();
            cloneItem.ItemAttributes = new();
            ItemAttributes.ForEach(item => { 
                cloneItem.ItemAttributes.Add(item.Clone()); 
            });
            return cloneItem;
        }

        public bool HasAttributes(ItemAttributesType attr)
        {
            return ItemAttributes.Exists(obj =>
                obj.itemAttributesType == attr);
        }

        public ItemAttributes GetAttributes(ItemAttributesType attr)
        {
            return ItemAttributes.Find(obj => 
                obj.itemAttributesType == attr);
        }

        public float GetAttributeValue(ItemAttributesType attr, float defaultValue = 0f)
        {
            var result = ItemAttributes.Find(obj => 
                obj.itemAttributesType == attr);
            
            return result.value != null ? result.value : defaultValue;
        }
    }
}