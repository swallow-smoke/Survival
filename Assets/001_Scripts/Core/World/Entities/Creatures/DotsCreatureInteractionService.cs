using AstraNope.Core.World.Entities.Interfaces;
using AstraNope.Data.Items;
using AstraNope.Contracts;
using Unity.Entities;
using UnityEngine;
using WorldBuilder.Entities.Creatures;

using AstraNope.Localization;
namespace AstraNope.Core.World.Entities.Creatures
{
    public sealed class DotsCreatureInteractionService : ICreatureInteractionService
    {
        private readonly IWorldCreatureGateway gateway;
        private readonly ICreatureToolSelector toolSelector;
        private readonly CreatureColorFoodCatalog colorFoods;

        private Entity focusedEntity = Entity.Null;
        private CreatureToolSelection selectedTool;
        private CreatureInteractAction action;
        private Vector3 focusOrigin;

        private Entity labelEntity = Entity.Null;
        private CreatureInteractAction labelAction;
        private CreatureGrade labelGrade;
        private int labelAttempts = -1;
        private string cachedLabel;

        private enum CreatureInteractAction : byte
        {
            None,
            Capture,
            Tame,
            ApplyColor,
            ApplyPattern,
            Inspect
        }

        public DotsCreatureInteractionService(IWorldCreatureGateway gateway, ICreatureToolSelector toolSelector,
            CreatureColorFoodCatalog colorFoods)
        {
            this.gateway = gateway;
            this.toolSelector = toolSelector;
            this.colorFoods = colorFoods;
        }

        public bool TryFocus(Vector3 origin, Vector3 direction, float distance, out CreatureInteractionFocus focus)
        {
            focus = default;
            if (!gateway.TryRaycast(origin, direction, distance, out Entity target, out float fraction) ||
                !gateway.TryGetInteractionInfo(target, out CreatureInteractionInfo info))
            {
                ClearFocus();
                return false;
            }

            focusedEntity = target;
            focusOrigin = origin;
            selectedTool = toolSelector?.Select() ?? new CreatureToolSelection(-1, 0);
            action = ResolveAction(info, selectedTool);

            if (labelEntity != target || labelAction != action || labelGrade != info.Grade ||
                labelAttempts != info.TameAttempts)
            {
                labelEntity = target;
                labelAction = action;
                labelGrade = info.Grade;
                labelAttempts = info.TameAttempts;
                cachedLabel = BuildLabel(info, action);
            }

            Vector3 hitPoint = origin + direction.normalized * (Mathf.Max(0f, distance) * Mathf.Clamp01(fraction));
            focus = new CreatureInteractionFocus(action != CreatureInteractAction.None &&
                                                 action != CreatureInteractAction.Inspect,
                cachedLabel, hitPoint, info.Grade);
            return true;
        }

        public bool InteractFocused()
        {
            if (focusedEntity == Entity.Null) return false;
            switch (action)
            {
                case CreatureInteractAction.Capture:
                    return gateway.TryCapture(focusedEntity, selectedTool.ItemId, selectedTool.Tier);
                case CreatureInteractAction.Tame:
                    return gateway.TryFeed(focusedEntity, selectedTool.ItemId, focusOrigin);
                case CreatureInteractAction.ApplyColor:
                    return colorFoods != null &&
                           colorFoods.TryGetColorFood(selectedTool.ItemId, out ColorFoodDefinition food) &&
                           gateway.TryRecolor(focusedEntity, food.defaultSlot, food.paletteId);
                case CreatureInteractAction.ApplyPattern:
                    return colorFoods != null &&
                           colorFoods.TryGetPatternFood(selectedTool.ItemId, out PatternFoodDefinition pattern) &&
                           gateway.TrySetPattern(focusedEntity, pattern.pattern, -1, pattern.strength);
                default:
                    return false;
            }
        }

        public void ClearFocus()
        {
            focusedEntity = Entity.Null;
            selectedTool = default;
            action = CreatureInteractAction.None;
            labelEntity = Entity.Null;
            labelAttempts = -1;
            cachedLabel = null;
        }

        private CreatureInteractAction ResolveAction(in CreatureInteractionInfo info,
            in CreatureToolSelection tool)
        {
            if (info.SizeClass == CreatureSizeClass.Large) return CreatureInteractAction.Inspect;

            bool isColorFood = colorFoods != null && colorFoods.TryGetColorFood(tool.ItemId, out _);
            bool isPatternFood = colorFoods != null && colorFoods.TryGetPatternFood(tool.ItemId, out _);

            if ((isColorFood || isPatternFood) && info.CanRecolor && info.IsTamed)
                return isColorFood ? CreatureInteractAction.ApplyColor : CreatureInteractAction.ApplyPattern;

            if (!info.IsTamed && info.CanFeed && !info.IsAlarmed &&
                (isColorFood || isPatternFood) == false && tool.ItemId >= 0)
                return CreatureInteractAction.Tame;

            if (EvaluateCapture(info, tool) == CreatureCaptureFailure.None) return CreatureInteractAction.Capture;
            if (!info.IsTamed && info.CanFeed && !info.IsAlarmed) return CreatureInteractAction.Tame;
            return CreatureInteractAction.Inspect;
        }

        private static CreatureCaptureFailure EvaluateCapture(in CreatureInteractionInfo info,
            in CreatureToolSelection tool)
        {
            if (!info.CanCapture || info.CaptureItemId < 0) return CreatureCaptureFailure.NotCapturable;
            if (info.IsTamed) return CreatureCaptureFailure.Tamed;
            if (info.RequiredToolItemId != CreatureInteractionRules.AnyItemId &&
                info.RequiredToolItemId != tool.ItemId) return CreatureCaptureFailure.RequiredToolMissing;
            if (tool.Tier < info.MinimumToolTier) return CreatureCaptureFailure.ToolTierTooLow;
            return CreatureCaptureFailure.None;
        }

        private static string BuildLabel(in CreatureInteractionInfo info, CreatureInteractAction action)
        {
            string named = $"{info.DisplayName} [{GradeLabel(info.Grade)}]";
            switch (action)
            {
                case CreatureInteractAction.Capture:
                    return L10n.F("k_5748000898", named);
                case CreatureInteractAction.Tame:
                {
                    int attemptCount = info.TameAttempts + 1;
                    return info.IsAlarmed
                        ? L10n.F("k_2c54f4020b", named)
                        : L10n.F("k_d09716d01f", named, attemptCount);
                }
                case CreatureInteractAction.ApplyColor:
                    return L10n.F("k_a5d5718699", named);
                case CreatureInteractAction.ApplyPattern:
                    return L10n.F("k_4af5ede4a1", named);
                default:
                    return info.SizeClass == CreatureSizeClass.Large ? named : L10n.F("k_5f6bbe53b7", named);
            }
        }

        private static string GradeLabel(CreatureGrade grade)
        {
            switch (grade)
            {
                case CreatureGrade.Rare: return L10n.T("k_f706e984d0");
                case CreatureGrade.Legendary: return L10n.T("k_d7a38fd8b5");
                default: return L10n.T("k_512989791c");
            }
        }
    }
}
