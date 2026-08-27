using AstraNope.Data.Items;
using UnityEngine;

namespace AstraNope.Contracts
{
    public interface IItemHolder
    {
        Transform Mount { get; }
        Item HeldItem { get; }
        ItemInstance HeldInstance { get; }
        GameObject HeldObject { get; }
        bool IsHolding { get; }

        bool TryEquip(Item item, ItemInstance instance = null);
        bool TryEquip(GameObject viewPrefab, Item item = null, ItemInstance instance = null);
        bool TryPerformPrimaryAction();
        void Unequip();
    }

    public interface IHeldItemAction
    {
        void OnEquipped(Item item, ItemInstance instance);
        bool TryPerformPrimaryAction(Item item, ItemInstance instance);
    }
}
