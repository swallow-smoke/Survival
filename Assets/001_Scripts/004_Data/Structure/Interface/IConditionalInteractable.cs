namespace _001_Scripts.Data.Structure.Interface
{
    public interface IConditionalInteractable : IInteractable
    {
        bool CanInteract();
        string RequirementLabel();
    }
}
