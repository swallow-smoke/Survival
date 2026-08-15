using _001_Scripts.Interface;
using Unity.Entities;
using UnityEngine;
using WorldBuilder.Entities.Creatures;

namespace _001_Scripts.Managers
{
    public sealed class DotsCreatureInteractionService : ICreatureInteractionService
    {
        private readonly IWorldCreatureGateway gateway;
        private readonly ICreatureToolSelector toolSelector;
        private Entity focusedEntity = Entity.Null;
        private CreatureToolSelection selectedTool;
        private bool canCapture;
        private bool canFeed;
        private Entity labelEntity = Entity.Null;
        private CreatureCaptureFailure labelFailure;
        private bool labelCanFeed;
        private CreatureGrade labelGrade;
        private string cachedLabel;

        public DotsCreatureInteractionService(IWorldCreatureGateway gateway, ICreatureToolSelector toolSelector)
        {
            this.gateway = gateway;
            this.toolSelector = toolSelector;
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
            selectedTool = toolSelector?.Select() ?? new CreatureToolSelection(-1, 0);
            CreatureCaptureFailure captureFailure = EvaluateCapture(info, selectedTool);
            canCapture = captureFailure == CreatureCaptureFailure.None;
            canFeed = info.CanFeed && (info.Affinity < info.MaximumAffinity);

            if (labelEntity != target || labelFailure != captureFailure || labelCanFeed != info.CanFeed ||
                labelGrade != info.Grade)
            {
                labelEntity = target;
                labelFailure = captureFailure;
                labelCanFeed = info.CanFeed;
                labelGrade = info.Grade;
                cachedLabel = BuildLabel(info, captureFailure);
            }

            Vector3 hitPoint = origin + direction.normalized * (Mathf.Max(0f, distance) * Mathf.Clamp01(fraction));
            focus = new CreatureInteractionFocus(canCapture || canFeed, cachedLabel, hitPoint, info.Grade);
            return true;
        }

        public bool InteractFocused()
        {
            if (focusedEntity == Entity.Null) return false;
            if (canCapture) return gateway.TryCapture(focusedEntity, selectedTool.ItemId, selectedTool.Tier);
            return canFeed && gateway.TryFeed(focusedEntity, selectedTool.ItemId);
        }

        public void ClearFocus()
        {
            focusedEntity = Entity.Null;
            selectedTool = default;
            canCapture = false;
            canFeed = false;
            labelEntity = Entity.Null;
            cachedLabel = null;
        }

        private static CreatureCaptureFailure EvaluateCapture(in CreatureInteractionInfo info,
            in CreatureToolSelection tool)
        {
            if (!info.CanCapture || info.CaptureItemId < 0) return CreatureCaptureFailure.NotCapturable;
            if (info.RequiredToolItemId != CreatureInteractionRules.AnyItemId &&
                info.RequiredToolItemId != tool.ItemId) return CreatureCaptureFailure.RequiredToolMissing;
            if (tool.Tier < info.MinimumToolTier) return CreatureCaptureFailure.ToolTierTooLow;
            return CreatureCaptureFailure.None;
        }

        private static string BuildLabel(in CreatureInteractionInfo info, CreatureCaptureFailure captureFailure)
        {
            string named = $"{info.DisplayName} [{info.Grade}]";
            switch (captureFailure)
            {
                case CreatureCaptureFailure.None:
                    return $"포획: {named}";
                case CreatureCaptureFailure.RequiredToolMissing:
                    return $"{named} - 포획 도구가 필요합니다";
                case CreatureCaptureFailure.ToolTierTooLow:
                    return $"{named} - 더 높은 등급의 도구가 필요합니다";
                default:
                    return info.CanFeed ? $"먹이 주기: {named}" : named;
            }
        }
    }
}
