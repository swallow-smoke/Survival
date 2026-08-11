using _001_Scripts.Data.Structure.Interface;
using UnityEngine;

namespace _001_Scripts.Entities
{
    /// <summary>Interaction behaviour owned by a parent entity; not an independent entity.</summary>
    public abstract class InteractableComponentBase : EntityFeature, IInteractionTarget,
        IInteractionPrompt
    {
        [Header("Interaction")]
        [SerializeField] private string displayLabel;
        [SerializeField] private string promptKey = "F";

        protected virtual string DefaultInteractionLabel => GetType().Name;
        public abstract void Interact();

        public virtual string GetLabel()
            => string.IsNullOrWhiteSpace(displayLabel) ? DefaultInteractionLabel : displayLabel;

        public string GetPromptKey() => string.IsNullOrWhiteSpace(promptKey) ? "F" : promptKey;
    }
}
