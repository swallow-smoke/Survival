using System;
using System.Collections.Generic;
using AstraNope.Data.Items.Modifiers;
using AstraNope.Data.Items.Types;
using UnityEngine;

namespace AstraNope.Data.Items
{
    [Serializable]
    public class ItemInstance
    {
        [Header("Identifier")]
        public int itemId;
        public int instanceId;
        
        [Header("Item Info")]
        public float durability;

        public ItemInstance(int itemId, int instanceId, float durability)
        {
            this.itemId = itemId;
            this.instanceId = instanceId;
            this.durability = durability;
        }
    }
}