using System;
using UnityEngine;
using WorldBuilder.Entities.Creatures;

namespace AstraNope.Data.Items
{
    [Serializable]
    public struct ColorFoodDefinition
    {
        [Tooltip("Item id of the crafted colour food.")]
        public int itemId;
        [Tooltip("Palette id this colour food resolves into. Must exist in the CreaturePaletteAsset.")]
        public int paletteId;
        [Tooltip("Colour slot this food targets when the player does not pick one explicitly.")]
        public CreatureColorSlot defaultSlot;
    }

    [Serializable]
    public struct PatternFoodDefinition
    {
        [Tooltip("Item id of the crafted pattern food.")]
        public int itemId;
        public CreaturePatternKind pattern;
        [Range(0f, 1f)] public float strength;
    }

    [CreateAssetMenu(menuName = "Survival/Creatures/Color Food Catalog", fileName = "ColorFoodCatalog")]
    public sealed class CreatureColorFoodCatalog : ScriptableObject
    {
        [SerializeField] private ColorFoodDefinition[] colorFoods = Array.Empty<ColorFoodDefinition>();
        [SerializeField] private PatternFoodDefinition[] patternFoods = Array.Empty<PatternFoodDefinition>();

        public int ColorFoodCount => colorFoods?.Length ?? 0;
        public int PatternFoodCount => patternFoods?.Length ?? 0;

        public ColorFoodDefinition GetColorFood(int index) => colorFoods[index];
        public PatternFoodDefinition GetPatternFood(int index) => patternFoods[index];

        public bool TryGetColorFood(int itemId, out ColorFoodDefinition definition)
        {
            ColorFoodDefinition[] entries = colorFoods ?? Array.Empty<ColorFoodDefinition>();
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].itemId != itemId) continue;
                definition = entries[i];
                return true;
            }
            definition = default;
            return false;
        }

        public bool TryGetPatternFood(int itemId, out PatternFoodDefinition definition)
        {
            PatternFoodDefinition[] entries = patternFoods ?? Array.Empty<PatternFoodDefinition>();
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].itemId != itemId) continue;
                definition = entries[i];
                return true;
            }
            definition = default;
            return false;
        }

        public bool IsColorItem(int itemId) => TryGetColorFood(itemId, out _) || TryGetPatternFood(itemId, out _);
    }
}
