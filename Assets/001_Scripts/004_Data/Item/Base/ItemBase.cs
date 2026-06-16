using System.Collections.Generic;
using _001_Scripts.Data.Item.Modifier;
using _001_Scripts.Type.Item;
using UnityEngine;

namespace _001_Scripts.Data.Item.Base
{
    public class ItemBase
    {
        [Header("Indentifier")]
        public int itemId;
        public int itemName;

        [Header("Info")]
        public string itemDesc;
        public ItemType itemType;
        public ItemGrade itemGrade;

        [Header("Modifier")] 
        public List<Modifier.Modifier> modifiers;

        public ItemBase Clone()
        {
            ItemBase cloneItem = (ItemBase)this.MemberwiseClone();
            cloneItem.modifiers = new();
            modifiers.ForEach(item => { 
                cloneItem.modifiers.Add(item.Clone()); 
            });
            return cloneItem;
        }
        
        public bool HasModifier(ModifierType attr)
        {
            return modifiers.Exists(obj =>
                obj.modifierType == attr);
        }

        public Modifier.Modifier GetAttributes(ModifierType attr)
        {
            return modifiers.Find(obj => 
                obj.modifierType == attr);
        }

        public float GetAttributeValue(ModifierType attr, float defaultValue = 0f)
        {
            var result = modifiers.Find(obj => 
                obj.modifierType == attr);
            
            return result.value != null ? result.value : defaultValue;
        }
    }
}