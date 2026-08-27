namespace AstraNope.Contracts.WorldObjects
{
    public interface IConditionalInteractable : IInteractable
    {
        bool CanInteract();
        string RequirementLabel();
    }

    public interface IConditionalInteractionTarget : IConditionalInteractable, IInteractionTarget { }
}
