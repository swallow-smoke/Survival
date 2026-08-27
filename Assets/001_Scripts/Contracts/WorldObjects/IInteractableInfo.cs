namespace AstraNope.Contracts.WorldObjects
{
    public interface IInteractableInfo
    {
        string GetLabel();
    }

    public interface IInteractionPrompt
    {
        string GetPromptKey();
    }
}
