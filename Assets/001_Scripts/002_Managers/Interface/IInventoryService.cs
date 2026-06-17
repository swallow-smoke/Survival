using System.Collections.Generic;
using _001_Scripts.Data.Item;

namespace _001_Scripts.Interface
{
    public interface IInventoryService
    {
        AddItemResult AddItem(int id, int count);

        void RemoveItem(int id, int count);
        void RemoveItem(Template item);
        void RemoveItem(Instance ins);
        
        bool HasItem(int id, int count = 1);
        bool HasItem(Template item, int count = 1);
        bool HasItem(Instance ins);

        IReadOnlyList<InventorySlot> GetAllItems();

        InventorySlot GetSlot(int index);
    }
}