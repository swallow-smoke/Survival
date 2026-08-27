using System.Collections.Generic;
using AstraNope.Data.Items;

namespace AstraNope.Contracts
{
    public interface IItemCatalog
    {
        bool TryGetItem(int id, out Item item);
    }

    public interface IInventoryReader
    {
        bool HasItem(int id, int count = 1);
        bool HasItem(Item item, int count = 1);
        bool HasItem(ItemInstance ins);
        IReadOnlyList<InventorySlot> GetAllItems();
        InventorySlot GetSlot(int index);
        int SlotCount { get; }
    }

    public interface IInventoryWriter
    {
        AddItemResult AddItem(int id, int count);
        void RemoveItem(int id, int count);
        void RemoveItem(Item item);
        void RemoveItem(ItemInstance ins);
    }

    public interface IInventoryActions
    {
        bool UseItem(int index);
        bool DropItem(int index, int count = 1);
        void SortItems();
    }

    public interface IHotbarReader
    {
        int HotbarSlotCount { get; }
        int SelectedHotbarIndex { get; }
        InventorySlot GetHotbarSlot(int index);
    }

    public interface IHotbarActions
    {
        bool SelectHotbar(int index);
        void CycleHotbar(int direction);
    }

    public interface IEquipmentReader
    {
        int EquipmentSlotCount { get; }
        EquipmentSlotType GetEquipmentSlotType(int index);
        InventorySlot GetEquipmentSlot(int index);
    }

    public interface IInventoryService : IInventoryReader, IInventoryWriter, IInventoryActions { }
}
