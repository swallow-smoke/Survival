using _001_Scripts.Data.Item;
using UnityEngine;

namespace _001_Scripts.Interface
{
    public interface IItemHolder
    {
        Transform Mount { get; }
        Item HeldItem { get; }
        Instance HeldInstance { get; }
        GameObject HeldObject { get; }
        bool IsHolding { get; }

        bool TryEquip(Item item, Instance instance = null);
        bool TryEquip(GameObject viewPrefab, Item item = null, Instance instance = null);
        bool TryPerformPrimaryAction();
        void Unequip();
    }

    public interface IHeldItemAction
    {
        void OnEquipped(Item item, Instance instance);
        bool TryPerformPrimaryAction(Item item, Instance instance);
    }
}
