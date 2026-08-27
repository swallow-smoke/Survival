namespace AstraNope.Contracts.WorldObjects
{
    public interface IInteractable
    {
        void Interact();
    }

    public interface IInteractionTarget : IInteractable, IInteractableInfo { }
}
