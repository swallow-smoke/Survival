using System.Collections.Generic;
using _001_Scripts.Data.Item;

namespace _001_Scripts.Interface
{
    public interface IInventoryService
    {
        void AddItem(int id, int count);

        void RemoveItem(int id, int count);
        
        bool HasItem(int id, int count);
        bool HasItem(Item item);

        IReadOnlyList<InventorySlotData> GetAllItems();
    }
}