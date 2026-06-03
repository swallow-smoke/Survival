using System.Collections.Generic;
using _001_Scripts.Data.BluePrint;

namespace _001_Scripts.Data.Message
{
    public readonly struct CraftResultMessage
    {
        public readonly int itemId;
        public readonly CraftMessageType msgType;
        public readonly List<RecipeEntry> missingItems;
        
        public CraftResultMessage(CraftMessageType type, int id, List<RecipeEntry> missing = null)
        {
            msgType = type;
            itemId = id;
            missingItems = missing;
        }
    }
    
    public enum CraftMessageType
    {
        Failed,
        Success
    }
}