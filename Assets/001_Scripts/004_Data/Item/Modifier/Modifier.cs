using System;
using _001_Scripts.Type.Item;
using UnityEngine;
using UnityEngine.Serialization;

namespace _001_Scripts.Data.Item.Modifier
{
    [Serializable]
    public class Modifier
    {
        public ModifierType modifierType;
        public float value;
        [Tooltip("Optional Field")] public float duration;
        [Tooltip("Optional Field")] public float cooldown;

        public Modifier Clone() => (Modifier)this.MemberwiseClone();
    }
}