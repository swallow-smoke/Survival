using System.Collections.Generic;

namespace _001_Scripts.Data.Message
{
    public readonly struct InvChangedMessage
    {
        public readonly List<int> changedKeys;
        public readonly List<int> changedHotbarKeys;
        public readonly List<int> changedEquipmentKeys;

        public InvChangedMessage(List<int> changedKeys, List<int> changedHotbarKeys = null,
            List<int> changedEquipmentKeys = null)
        {
            this.changedKeys = changedKeys ?? new List<int>();
            this.changedHotbarKeys = changedHotbarKeys ?? new List<int>();
            this.changedEquipmentKeys = changedEquipmentKeys ?? new List<int>();
        }
    }
}
