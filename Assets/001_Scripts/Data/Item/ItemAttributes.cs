using _001_Scripts.Type;

namespace _001_Scripts.Data
{
    [System.Serializable]
    public class ItemAttributes
    {
        public ItemAttributesType itemAttributesType;
        public float value;
        public float duration;
        public float cooldown;

        public ItemAttributes Clone() => (ItemAttributes)this.MemberwiseClone();
    }
}