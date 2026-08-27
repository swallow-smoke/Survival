using System.Collections.Generic;

namespace AstraNope.Data.Messages
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