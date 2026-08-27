using System;
using System.Collections.Generic;
using UnityEngine;

namespace AstraNope.UI.Components
{
    public readonly struct SimpleRadialIngredientData
    {
        public readonly Sprite Icon;
        public readonly string Glyph;
        public readonly string Name;
        public readonly int Count;

        public SimpleRadialIngredientData(Sprite icon, string glyph, string name, int count)
        {
            Icon = icon;
            Glyph = glyph;
            Name = name;
            Count = count;
        }
    }

    public sealed class SimpleRadialRecipeTooltipData
    {
        public readonly string Description;
        public readonly IReadOnlyList<SimpleRadialIngredientData> Ingredients;

        public SimpleRadialRecipeTooltipData(string description,
            IReadOnlyList<SimpleRadialIngredientData> ingredients)
        {
            Description = description;
            Ingredients = ingredients ?? Array.Empty<SimpleRadialIngredientData>();
        }
    }

    public readonly struct SimpleRadialEntry
    {
        public readonly string Id;
        public readonly string Icon;
        public readonly string Label;
        public readonly bool Interactable;
        public readonly Action Selected;
        public readonly string Tooltip;
        public readonly Action SecondarySelected;
        public readonly SimpleRadialRecipeTooltipData RecipeTooltip;

        public SimpleRadialEntry(string id, string icon, string label, Action selected, bool interactable = true,
            string tooltip = null, Action secondarySelected = null,
            SimpleRadialRecipeTooltipData recipeTooltip = null)
        {
            Id = id;
            Icon = icon;
            Label = label;
            Selected = selected;
            Interactable = interactable;
            Tooltip = tooltip;
            SecondarySelected = secondarySelected;
            RecipeTooltip = recipeTooltip;
        }
    }
}