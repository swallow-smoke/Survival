using System.Collections.Generic;

namespace _001_Scripts.Data.Message
{
    public readonly struct InvMessage
    {
        public readonly InvMessageType msgType;
        public readonly Item.Item item;

        public InvMessage(InvMessageType msgType, Item.Item items)
        {
            this.msgType = msgType;
            this.item = items;  
        }
    }

    public enum InvMessageType
    {
        Added,
        Removed
    }
}