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
            var result = (Attributes)MemberwiseClone();
            result.modifiers = new List<Modifier.Modifier>();
            if (modifiers != null)
                foreach (var modifier in modifiers)
                    if (modifier != null) result.modifiers.Add(modifier.Clone());
            return result;
        }
    }
}
