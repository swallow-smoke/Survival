using System.Collections.Generic;

namespace _001_Scripts.Data.Message
{
    public readonly struct CraftReqMessage
    {
        public readonly string itemName;
        
        public CraftReqMessage(string name)
        {
            itemName = name;
        }
    }
}