using System;
using System.Collections.Generic;
using _001_Scripts.Data.Item.Modifier;
using _001_Scripts.Type.Item;
using UnityEngine;

namespace _001_Scripts.Data.Item
{
    [Serializable]
    public class Template
    {
        [Header("Identifier")]
        public int itemId;
        public string itemName;
        
        [Header("Item Info")]
        public string itemDesc;
        public ItemGrade itemGrade;
        public ItemType itemType;
        public float weight;
        public List<Attributes.Attributes> ItemAttributes;

        public Template Clone()
        {
            Template cloneInstance = (Template)this.MemberwiseClone();
            cloneInstance.ItemAttributes = new();
            this.ItemAttributes.ForEach(item => 
            {
                cloneInstance.ItemAttributes.Add(item.Clone()); 
            });
            
            return cloneInstance;
        }

        public bool HasAttribute(AttributesType type) 
            => ItemAttributes.Exists(a => a.attrType == type);

        public float GetModifierValue(AttributesType attrType, ModifierType modType, float defaultValue = 0f)
        {
            var attr = ItemAttributes.Find(a => a.attrType == attrType);
            if (attr == null) return defaultValue;
        
            var mod = attr.modifiers.Find(m => m.modifierType == modType);
            return mod != null ? mod.value : defaultValue;
        }
    }
}