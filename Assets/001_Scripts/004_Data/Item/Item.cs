using System;
using System.Collections.Generic;
using _001_Scripts.Type.Item;
using UnityEngine;

namespace _001_Scripts.Data.Item
{
    [Serializable]
    public sealed class Item
    {
        [Header("Identifier")]
        public int itemId;
        public string itemName;

        [Header("Item Info")]
        public string itemDesc;
        public ItemGrade itemGrade;
        public ItemType itemType;
        public float weight;
        public List<Attributes.Attributes> ItemAttributes = new();

        [NonSerialized] private List<IItemFeature> _features;

        public ItemRole Role
        {
            get
            {
                EnsureFeatures();
                if (HasFeature<ITool>()) return ItemRole.Tool;
                if (HasFeature<IUsable>()) return ItemRole.Usable;
                if (HasFeature<IEquippable>()) return ItemRole.Equipment;
                if (itemType == ItemType.materials) return ItemRole.Material;
                return ItemRole.Other;
            }
        }

        public Item Clone()
        {
            var clone = (Item)MemberwiseClone();
            clone.ItemAttributes = new List<Attributes.Attributes>();
            if (ItemAttributes != null)
                foreach (var attribute in ItemAttributes)
                    if (attribute != null) clone.ItemAttributes.Add(attribute.Clone());
            clone._features = null;
            return clone;
        }

        public bool HasFeature<T>() where T : class, IItemFeature
            => TryGetFeature<T>(out _);

        public bool TryGetFeature<T>(out T feature) where T : class, IItemFeature
        {
            EnsureFeatures();
            foreach (var candidate in _features)
            {
                if (candidate is not T match) continue;
                feature = match;
                return true;
            }

            feature = null;
            return false;
        }

        public IReadOnlyList<IItemFeature> GetFeatures()
        {
            EnsureFeatures();
            return _features;
        }

        internal bool HasAttribute(AttributesType type)
            => ItemAttributes != null && ItemAttributes.Exists(attribute => attribute != null && attribute.attrType == type);

        internal float GetModifierValue(AttributesType attributeType, ModifierType modifierType,
            float defaultValue = 0f)
        {
            var attribute = ItemAttributes?.Find(candidate => candidate != null && candidate.attrType == attributeType);
            var modifier = attribute?.modifiers?.Find(candidate => candidate != null && candidate.modifierType == modifierType);
            return modifier != null ? modifier.value : defaultValue;
        }

        private void EnsureFeatures()
        {
            if (_features != null) return;
            _features = new List<IItemFeature>();
            if (HasAttribute(AttributesType.Stackable)) _features.Add(new Stackable(this));
            if (HasAttribute(AttributesType.Equippable)) _features.Add(new Equippable(this));
            if (HasAttribute(AttributesType.Consumable)) _features.Add(new Usable(this));
            if (HasAttribute(AttributesType.Harvestable)) _features.Add(new Tool(this));
            if (HasAttribute(AttributesType.QuickSlottable)) _features.Add(new QuickSlottable(this));
            if (HasAttribute(AttributesType.Repairable)) _features.Add(new RepairableItem(this));
            if (HasAttribute(AttributesType.Explosive)) _features.Add(new ExplosiveItem(this));
            if (HasAttribute(AttributesType.Scannable)) _features.Add(new ScannableItem(this));
        }
    }
}
