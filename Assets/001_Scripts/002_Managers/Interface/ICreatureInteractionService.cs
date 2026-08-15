using UnityEngine;
using WorldBuilder.Entities.Creatures;

namespace _001_Scripts.Interface
{
    public readonly struct CreatureToolSelection
    {
        public readonly int ItemId;
        public readonly byte Tier;

        public CreatureToolSelection(int itemId, byte tier)
        {
            ItemId = itemId;
            Tier = tier;
        }
    }

    public interface ICreatureToolSelector
    {
        CreatureToolSelection Select();
    }

    public readonly struct CreatureInteractionFocus
    {
        public readonly bool CanInteract;
        public readonly string Label;
        public readonly Vector3 HitPoint;
        public readonly CreatureGrade Grade;

        public CreatureInteractionFocus(bool canInteract, string label, Vector3 hitPoint, CreatureGrade grade)
        {
            CanInteract = canInteract;
            Label = label ?? string.Empty;
            HitPoint = hitPoint;
            Grade = grade;
        }
    }

    public interface ICreatureInteractionService
    {
        bool TryFocus(Vector3 origin, Vector3 direction, float distance, out CreatureInteractionFocus focus);
        bool InteractFocused();
        void ClearFocus();
    }
}
