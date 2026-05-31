using System.Collections.Generic;

namespace _001_Scripts.Data.Message
{
    public readonly struct InvReqMessage
    {
        public readonly InvMessageType msgType;
        public readonly Item.Item item;
        public readonly int count;

        public InvReqMessage(InvMessageType msgType, Item.Item items, int count)
        {
            this.msgType = msgType;
            this.item = items;
            this.count = count;
        }
    }

    public enum InvMessageType
    {
        Added,
        Removed
    }
}