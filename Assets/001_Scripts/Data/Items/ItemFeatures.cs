using AstraNope.Data.Items.Types;

namespace AstraNope.Data.Items
{
    public enum ItemRole
    {
        Material,
        Tool,
        Usable,
        Equipment,
        Other
    }

    public enum EquipmentSlotType
    {
        None,
        Head,
        Body,
        Legs,
        Feet,
        UpgradeChip
    }

    public interface IItemFeature
    {
        Item Item { get; }
    }

    public interface IStackable : IItemFeature
    {
        int MaxStack { get; }
    }

    public interface IEquippable : IItemFeature
    {
        float MaxDurability { get; }
    }

    public interface IEquipmentItem : IItemFeature
    {
        EquipmentSlotType SlotType { get; }
    }

    public interface ITool : IItemFeature
    {
        float HarvestRate { get; }
        float MaxDurability { get; }
    }

    public interface IUsable : IItemFeature
    {
        void Use(IItemUseTarget target);
    }

    public interface IItemUseTarget
    {
        void RestoreHealth(float amount);
        void ModifyOxygen(float amount);
        void ModifyFood(float amount);
        void ModifyWater(float amount);
    }

    public interface IQuickSlottable : IItemFeature { }
    public interface IRepairableItem : IItemFeature { }
    public interface IExplosiveItem : IItemFeature { float Power { get; } }
    public interface IScannableItem : IItemFeature { float Range { get; } }
    public interface IHoldable : IItemFeature { UnityEngine.GameObject FirstPersonPrefab { get; } }
    public interface IBuildTool : IItemFeature { }

    public abstract class ItemFeature : IItemFeature
    {
        protected ItemFeature(Item item) => Item = item;
        public Item Item { get; }
    }

    public sealed class Stackable : ItemFeature, IStackable
    {
        public Stackable(Item item) : base(item) { }
        public int MaxStack => UnityEngine.Mathf.Max(1, UnityEngine.Mathf.RoundToInt(
            Item.GetModifierValue(AttributesType.Stackable, ModifierType.MaxStack, 1f)));
    }

    public sealed class Equippable : ItemFeature, IEquippable
    {
        public Equippable(Item item) : base(item) { }
        public float MaxDurability => UnityEngine.Mathf.Max(0f,
            Item.GetModifierValue(AttributesType.Equippable, ModifierType.DurabilityMax, 0f));
    }

    public sealed class EquipmentItem : ItemFeature, IEquipmentItem
    {
        public EquipmentItem(Item item) : base(item) { }
        public EquipmentSlotType SlotType => Item.equipmentSlot;
    }

    public sealed class Tool : ItemFeature, ITool
    {
        public Tool(Item item) : base(item) { }
        public float HarvestRate => UnityEngine.Mathf.Max(0f,
            Item.GetModifierValue(AttributesType.Harvestable, ModifierType.HarvestRate, 1f));
        public float MaxDurability => Item.TryGetFeature<IEquippable>(out var equippable)
            ? equippable.MaxDurability
            : 0f;
    }

    public sealed class Usable : ItemFeature, IUsable
    {
        public Usable(Item item) : base(item) { }

        public void Use(IItemUseTarget target)
        {
            if (target == null) return;
            target.RestoreHealth(Item.GetModifierValue(AttributesType.Consumable, ModifierType.HealAmount));
            target.ModifyOxygen(Item.GetModifierValue(AttributesType.Consumable, ModifierType.OxygenAmount));
            target.ModifyFood(Item.GetModifierValue(AttributesType.Consumable, ModifierType.FoodValue));
            target.ModifyWater(Item.GetModifierValue(AttributesType.Consumable, ModifierType.WaterValue));
        }
    }

    public sealed class QuickSlottable : ItemFeature, IQuickSlottable
    {
        public QuickSlottable(Item item) : base(item) { }
    }

    public sealed class RepairableItem : ItemFeature, IRepairableItem
    {
        public RepairableItem(Item item) : base(item) { }
    }

    public sealed class ExplosiveItem : ItemFeature, IExplosiveItem
    {
        public ExplosiveItem(Item item) : base(item) { }
        public float Power => UnityEngine.Mathf.Max(0f,
            Item.GetModifierValue(AttributesType.Explosive, ModifierType.ExplosivePower));
    }

    public sealed class ScannableItem : ItemFeature, IScannableItem
    {
        public ScannableItem(Item item) : base(item) { }
        public float Range => UnityEngine.Mathf.Max(0f,
            Item.GetModifierValue(AttributesType.Scannable, ModifierType.ScanRange));
    }

    public sealed class BuildTool : ItemFeature, IBuildTool
    {
        public BuildTool(Item item) : base(item) { }
    }

    public sealed class Holdable : ItemFeature, IHoldable
    {
        public Holdable(Item item) : base(item) { }
        public UnityEngine.GameObject FirstPersonPrefab => Item.firstPersonPrefab;
    }
}
