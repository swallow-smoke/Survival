using System.Collections.Generic;

namespace _001_Scripts.Data.Message
{
    public readonly struct CraftResultMessage
    {
        public readonly string itemName;
        public readonly CraftMessageType msgType;
        public readonly List<Item.Item> missingItems;
        
        public CraftResultMessage(CraftMessageType type, string name, List<Item.Item> missing = null)
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