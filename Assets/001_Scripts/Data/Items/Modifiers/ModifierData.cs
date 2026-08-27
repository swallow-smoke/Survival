using System;
using System.Collections.Generic;
using AstraNope.Data.Items.Modifiers;
using AYellowpaper.SerializedCollections;

namespace AstraNope.Data.Items.Modifiers
{
    [Serializable]
    public class ModifierData
    {
        public string type;
        public ModifierTiming timing;
        
        [SerializedDictionary("name", "value")]
        public SerializedDictionary<string, float> values;
    }
}