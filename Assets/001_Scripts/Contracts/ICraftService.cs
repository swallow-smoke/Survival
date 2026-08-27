namespace AstraNope.Contracts
{
    public interface ICraftingService
    {
        void Craft(string itemName);
    }

    public interface ICraftService : ICraftingService { }
}
