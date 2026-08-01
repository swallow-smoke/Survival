using UnityEngine;

namespace _001_Scripts.Interface
{
    public readonly struct ResourceInteractionFocus
    {
        public readonly bool CanInteract;
        public readonly string Label;

        public ResourceInteractionFocus(bool canInteract, string label)
        {
            CanInteract = canInteract;
            Label = label ?? string.Empty;
        }
    }

    public interface IResourceInteractionService
    {
        bool TryFocus(Vector3 origin, Vector3 direction, float distance, out ResourceInteractionFocus focus);
        bool InteractFocused();
        void ClearFocus();
    }
}
