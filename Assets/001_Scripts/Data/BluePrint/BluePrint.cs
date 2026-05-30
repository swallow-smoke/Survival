using System.Collections.Generic;

namespace _001_Scripts.Data.BluePrint
{
    [System.Serializable]
    public class BluePrint
    {
        public Item.Item resultCraft;
        public List<RecipeEntry> recipe;
        public float craftTime;
        public int requiredLevel;
        public bool isUnlocked;
        public string bluePrintName;
        public int bluePrintId;

        public BluePrint Clone()
        {
            BluePrint cloned = (BluePrint)this.MemberwiseClone();
            cloned.recipe = new();
            cloned.resultCraft = resultCraft.Clone();
            recipe.ForEach(item =>
            {
                cloned.recipe.Add(item.Clone());
            });
            return cloned;
        }
    }
}