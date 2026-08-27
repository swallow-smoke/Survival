using System.Collections.Generic;

namespace AstraNope.Data.Blueprints
{
    [System.Serializable]
    public class BluePrint
    {
        public int resultCraft;
        public List<RecipeEntry> recipe;
        public float craftTime;
        public int requiredLevel;
        public bool isUnlocked;
        public int unlockProgress;
        public int unlockRequired = 1;
        [UnityEngine.Tooltip("Slash-separated radial category path, e.g. Materials/Metal/Iron")]
        public string categoryPath;
        [UnityEngine.Tooltip("Optional Resources.Load path for the circular blueprint icon.")]
        public string iconResource;
        public string bluePrintName;
        public int bluePrintId;

        public BluePrint Clone()
        {
            BluePrint cloned = (BluePrint)this.MemberwiseClone();
            cloned.recipe = new();
            recipe?.ForEach(item =>
            {
                cloned.recipe.Add(item.Clone());
            });
            return cloned;
        }
    }
}
