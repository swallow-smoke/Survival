using UnityEngine;

namespace _001_Scripts.Base
{
    [System.Serializable]
    [System.Obsolete("Legacy structure data. Add Entity and Structure components to scene objects.")]
    public abstract class StructureBase
    {
        public int maxHP;
        public float weight;
        public string structureName;
        public int structureId;

        public bool canRotate;
        public bool isUnlocked;
    }
}
