using System.Collections.Generic;

namespace AstraNope.Data.Messages
{
    public readonly struct InventoryChangedMessage
    {
        public readonly List<int> changedKeys;
        public readonly List<int> changedHotbarKeys;
        public readonly List<int> changedEquipmentKeys;

        public InventoryChangedMessage(List<int> changedKeys, List<int> changedHotbarKeys = null,
            List<int> changedEquipmentKeys = null)
        {
            this.changedKeys = changedKeys ?? new List<int>();
            this.changedHotbarKeys = changedHotbarKeys ?? new List<int>();
            this.changedEquipmentKeys = changedEquipmentKeys ?? new List<int>();
        }
    }
}
