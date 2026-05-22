using System.Collections.Generic;

namespace _001_Scripts.Data.Message
{
    public class InvMessage
    {
        public InvMessageType msgType;
        public List<Item.Item> items;
        public string desc;
    }

    public enum InvMessageType
    {
        Added,
        Removed,
        Updated,
        Revert
    }
}