namespace _001_Scripts.Data.BluePrint
{
    [System.Serializable]
    public class RecipeEntry
    {
        public int item;
        public int count;

        public RecipeEntry Clone()
        {
            RecipeEntry cloneItem = (RecipeEntry)this.MemberwiseClone();
            return cloneItem;
        }
    }
}