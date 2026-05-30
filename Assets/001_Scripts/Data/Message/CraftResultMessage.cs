using System.Collections.Generic;
using _001_Scripts.Data.BluePrint;

namespace _001_Scripts.Data.Message
{
    public readonly struct CraftResultMessage
    {
        public readonly string itemName;
        public readonly CraftMessageType msgType;
        public readonly List<RecipeEntry> missingItems;
        
        public CraftResultMessage(CraftMessageType type, string name, List<RecipeEntry> missing = null)
        {
            msgType = type;
            itemName = name;
            missingItems = missing;
        }
    }
    
    public enum CraftMessageType
    {
        Failed,
        Success
    }
}