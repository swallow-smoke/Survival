using System;
using System.Collections.Generic;
using _001_Scripts.Data.Item.Base;
using _001_Scripts.Type.Item;
using UnityEngine;

namespace _001_Scripts.Data.Item
{
    [Serializable]
    public class Instance
    {
        [Header("Identifier")]
        public int itemId;
        public int instanceId;
        
        [Header("Item Info")]
        public float durability;

        public Instance(int itemId, int instanceId, float durability)
        {
            this.itemId = itemId;
            this.instanceId = instanceId;
            this.durability = durability;
        }
    }
}