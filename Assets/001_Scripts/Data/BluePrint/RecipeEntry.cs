namespace _001_Scripts.Data.BluePrint
{
    [System.Serializable]
    public class RecipeEntry
    {
        public Item.Item item;
        public int count;

        public RecipeEntry Clone()
        {
            RecipeEntry cloneItem = (RecipeEntry)this.MemberwiseClone();
            cloneItem.item = item.Clone();
            return cloneItem;
        }
    }
}