using System;
using System.Collections.Generic;
using _001_Scripts.Data.Item.Modifier;
using _001_Scripts.Type.Item;

namespace _001_Scripts.Data.Item.Attributes
{
    [Serializable]
    public class Attributes
    {
        public AttributesType attrType;
        public List<Modifier.Modifier> modifiers;


        public Attributes Clone()
        {
            var result = (Attributes)this.MemberwiseClone();
            modifiers.ForEach(item => { result.modifiers.Add(item.Clone()); });
            return result;
        }
    }
}