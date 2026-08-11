namespace _001_Scripts.Interface
{
    public interface ICraftingService
    {
        void Craft(string itemName);
    }

    public interface ICraftService : ICraftingService { }
}
