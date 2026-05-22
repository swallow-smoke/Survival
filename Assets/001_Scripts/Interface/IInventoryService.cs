using _001_Scripts.Data.Item;

namespace _001_Scripts.Interface
{
    public interface IInventoryService
    {
        void AddItem(int id);
        void AddItem(string name);
        void AddItem(Item item);

        void RemoveItem(int id);
        void RemoveItem(string name);
        void RemoveItem(Item item);
        
        bool HasItem(string name);
        bool HasItem(int id);
        bool HasItem(Item item);
    }
}