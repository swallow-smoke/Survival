using System.Collections.Generic;
using AstraNope.Data.Blueprints;

namespace AstraNope.Data.Messages
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