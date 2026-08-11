namespace _001_Scripts.Data.Structure.Interface
{
    public interface IInteractable
    {
        void Interact();
    }

    public interface IInteractionTarget : IInteractable, IInteractableInfo { }
}
