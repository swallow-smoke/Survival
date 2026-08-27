using System;
using AstraNope.Data.Items.Types;
using UnityEngine;
using UnityEngine.Serialization;

namespace AstraNope.Data.Items.Modifiers
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