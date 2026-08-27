using System.Collections.Generic;
using AstraNope.Data.Items;

namespace AstraNope.Data.Messages
{
    public readonly struct InventoryRequestMessage
    {
        public readonly InvMessageType msgType;
        public readonly int item;
        public readonly int count;

        public InventoryRequestMessage(InvMessageType msgType, int item, int count)
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