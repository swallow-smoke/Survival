using System.Collections.Generic;

namespace _001_Scripts.Data.Message
{
    public readonly struct InvChangedMessage
    {
        public readonly List<int> changedKeys;
        public readonly bool isStackable;

        public InvChangedMessage(List<int> changedKeys, bool isStackable)
        {
            this.changedKeys = changedKeys;
            this.isStackable = isStackable;
        }
    }
}