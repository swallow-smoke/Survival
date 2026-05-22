using System.Collections.Generic;
using _001_Scripts.Type;

namespace _001_Scripts.Data
{
    [System.Serializable]
    public class Item
    {
        public string itemName;
        public int itemId;
        public string itemDesc;
        public ItemGrade itemGrade;
        public ItemType itemType;
        public List<ItemAttributes> ItemAttributes;

        public Item Clone()
        {
            Item cloneItem = (Item)this.MemberwiseClone();
            cloneItem.ItemAttributes = new();
            ItemAttributes.ForEach(
                item => cloneItem.ItemAttributes.Add(item.Clone()));
            return cloneItem;
        }
    }
}