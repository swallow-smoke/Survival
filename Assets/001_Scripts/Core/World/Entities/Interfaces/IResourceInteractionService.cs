using UnityEngine;

namespace AstraNope.Core.World.Entities.Interfaces
{
    public readonly struct ResourceInteractionFocus
    {
        public readonly bool CanInteract;
        public readonly string Label;
        public readonly Vector3 HitPoint;

        public ResourceInteractionFocus(bool canInteract, string label, Vector3 hitPoint)
        {
            CanInteract = canInteract;
            Label = label ?? string.Empty;
            HitPoint = hitPoint;
        }
    }

    public interface IResourceInteractionService
    {
        bool TryFocus(Vector3 origin, Vector3 direction, float distance, out ResourceInteractionFocus focus);
        bool InteractFocused();
        void ClearFocus();
    }
}
