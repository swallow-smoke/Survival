using System;
using System.Collections.Generic;
using _001_Scripts.Data.Item.Base;
using AYellowpaper.SerializedCollections;

namespace _001_Scripts.Data.Item.Modifier
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