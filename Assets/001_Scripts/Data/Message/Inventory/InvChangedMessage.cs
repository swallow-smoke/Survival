using System.Collections.Generic;

namespace _001_Scripts.Data.Message
{
    public readonly struct InvChangedMessage
    {
        public readonly List<int> changedKeys;

        public InvChangedMessage(List<int> changedKeys)
        {
            this.changedKeys = changedKeys;
        }
    }
}