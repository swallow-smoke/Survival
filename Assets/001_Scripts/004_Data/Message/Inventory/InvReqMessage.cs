using System.Collections.Generic;
using _001_Scripts.Data.Item;

namespace _001_Scripts.Data.Message
{
    public readonly struct InvReqMessage
    {
        public readonly InvMessageType msgType;
        public readonly int item;
        public readonly int count;

        public InvReqMessage(InvMessageType msgType, int item, int count)
        {
            this.msgType = msgType;
            this.item = item;
            this.count = count;
        }
    }

    public enum InvMessageType
    {
        Added,
        Removed
    }
}