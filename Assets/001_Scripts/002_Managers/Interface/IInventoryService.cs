using System.Collections.Generic;
using _001_Scripts.Data.Item;

namespace _001_Scripts.Interface
{
    public interface IInventoryReader
    {
        bool HasItem(int id, int count = 1);
        bool HasItem(Item item, int count = 1);
        bool HasItem(Instance ins);
        IReadOnlyList<InventorySlot> GetAllItems();
        InventorySlot GetSlot(int index);
        int SlotCount { get; }
    }

    public interface IInventoryWriter
    {
        AddItemResult AddItem(int id, int count);
        void RemoveItem(int id, int count);
        void RemoveItem(Item item);
        void RemoveItem(Instance ins);
    }

    public interface IInventoryActions
    {
        bool UseItem(int index);
        bool DropItem(int index, int count = 1);
        void SortItems();
    }

    public interface IInventoryService : IInventoryReader, IInventoryWriter, IInventoryActions { }
}
