using System;
using System.Collections.Generic;
using Modifier = AstraNope.Data.Items.Modifiers;
using AstraNope.Data.Items.Types;

namespace AstraNope.Data.Items.Attributes
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
