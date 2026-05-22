using _001_Scripts.Type.Item;

namespace _001_Scripts.Data.Item
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