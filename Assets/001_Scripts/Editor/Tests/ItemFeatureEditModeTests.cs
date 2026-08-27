#if UNITY_EDITOR
using System.Collections.Generic;
using AstraNope.Gameplay.Player;
using AstraNope.Data.Items;
using AstraNope.Data.Items.Types;
using NUnit.Framework;
using ItemAttribute = AstraNope.Data.Items.Attributes.Attributes;
using ItemModifier = AstraNope.Data.Items.Modifiers.Modifier;

namespace AstraNope.Editor.Tests
{
    public sealed class ItemFeatureEditModeTests
    {
        [Test]
        public void Tool_CanComposeEquipmentAndQuickSlotFeatures()
        {
            var item = CreateItem(
                Attribute(AttributesType.Harvestable, ModifierType.HarvestRate, 2.5f),
                Attribute(AttributesType.Equippable, ModifierType.DurabilityMax, 80f),
                Attribute(AttributesType.QuickSlottable));

            Assert.That(item.Role, Is.EqualTo(ItemRole.Tool));
            Assert.That(item.TryGetFeature<ITool>(out var tool), Is.True);
            Assert.That(tool.HarvestRate, Is.EqualTo(2.5f));
            Assert.That(tool.MaxDurability, Is.EqualTo(80f));
            Assert.That(item.HasFeature<IEquippable>(), Is.True);
            Assert.That(item.HasFeature<IQuickSlottable>(), Is.True);
        }

        [Test]
        public void Usable_AppliesConfiguredValuesThroughUseTargetContract()
        {
            var consumable = new ItemAttribute
            {
                attrType = AttributesType.Consumable,
                modifiers = new List<ItemModifier>
                {
                    Modifier(ModifierType.HealAmount, 10f),
                    Modifier(ModifierType.OxygenAmount, 20f),
                    Modifier(ModifierType.FoodValue, 30f),
                    Modifier(ModifierType.WaterValue, 40f)
                }
            };
            var item = CreateItem(consumable, Attribute(AttributesType.Stackable, ModifierType.MaxStack, 6f));
            var target = new FakeUseTarget();

            Assert.That(item.Role, Is.EqualTo(ItemRole.Usable));
            Assert.That(item.TryGetFeature<IUsable>(out var usable), Is.True);
            Assert.That(item.TryGetFeature<IStackable>(out var stackable), Is.True);
            Assert.That(stackable.MaxStack, Is.EqualTo(6));

            usable.Use(target);

            Assert.That(target.Health, Is.EqualTo(10f));
            Assert.That(target.Oxygen, Is.EqualTo(20f));
            Assert.That(target.Food, Is.EqualTo(30f));
            Assert.That(target.Water, Is.EqualTo(40f));
        }

        [Test]
        public void MaterialWithoutBehavior_RemainsMaterial()
        {
            var item = CreateItem(Attribute(AttributesType.Stackable, ModifierType.MaxStack, 10f));

            Assert.That(item.Role, Is.EqualTo(ItemRole.Material));
            Assert.That(item.HasFeature<IStackable>(), Is.True);
            Assert.That(item.HasFeature<ITool>(), Is.False);
            Assert.That(item.HasFeature<IUsable>(), Is.False);
        }

        [Test]
        public void FirstPersonHolder_SpawnsViewAtMountAndDisablesPhysics()
        {
            var holderObject = new UnityEngine.GameObject("Holder");
            var viewPrefab = new UnityEngine.GameObject("ToolView");
            viewPrefab.transform.localScale = new UnityEngine.Vector3(2f, 2f, 2f);
            viewPrefab.AddComponent<UnityEngine.BoxCollider>();
            viewPrefab.AddComponent<UnityEngine.Rigidbody>();
            var item = CreateItem(Attribute(AttributesType.QuickSlottable));
            item.firstPersonPrefab = viewPrefab;

            try
            {
                var holder = holderObject.AddComponent<FirstPersonItemHolder>();

                Assert.That(item.HasFeature<IHoldable>(), Is.True);
                Assert.That(holder.TryEquip(item), Is.True);
                Assert.That(holder.IsHolding, Is.True);
                Assert.That(holder.HeldItem, Is.SameAs(item));
                Assert.That(holder.HeldObject.transform.parent, Is.SameAs(holder.transform));
                Assert.That(holder.HeldObject.transform.localPosition, Is.EqualTo(UnityEngine.Vector3.zero));
                Assert.That(holder.HeldObject.transform.localRotation, Is.EqualTo(UnityEngine.Quaternion.identity));
                Assert.That(holder.HeldObject.GetComponent<UnityEngine.Collider>().enabled, Is.False);
                Assert.That(holder.HeldObject.GetComponent<UnityEngine.Rigidbody>().isKinematic, Is.True);

                holder.Unequip();
                Assert.That(holder.IsHolding, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(holderObject);
                UnityEngine.Object.DestroyImmediate(viewPrefab);
            }
        }

        [Test]
        public void FirstPersonHolder_UsesSceneAuthoredProceduralMotionWithoutAnimatorController()
        {
            var holderObject = new UnityEngine.GameObject("Player");
            var cameraObject = new UnityEngine.GameObject("Camera");
            var rigObject = new UnityEngine.GameObject("FirstPersonItemRig");
            var viewPrefab = new UnityEngine.GameObject("ToolView");
            try
            {
                rigObject.transform.SetParent(cameraObject.transform, false);
                rigObject.transform.localPosition = new UnityEngine.Vector3(.28f, -.25f, .52f);
                var motion = rigObject.AddComponent<FirstPersonItemMotion>();
                motion.Configure(rigObject.transform);
                var holder = holderObject.AddComponent<FirstPersonItemHolder>();
                holder.Configure(rigObject.transform, motion);

                Assert.That(holder.TryEquip(viewPrefab), Is.True);
                Assert.That(holder.HeldObject.transform.parent, Is.SameAs(rigObject.transform));
                Assert.That(holder.TryPerformPrimaryAction(), Is.True);
                Assert.That(holder.TryPerformHarvestAction(), Is.True);

                holder.Unequip();
                Assert.That(holder.IsHolding, Is.False);
                Assert.That(rigObject.transform.localPosition,
                    Is.EqualTo(new UnityEngine.Vector3(.28f, -.25f, .52f)));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(holderObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(viewPrefab);
            }
        }

        private static Item CreateItem(params ItemAttribute[] attributes) => new()
        {
            itemType = ItemType.materials,
            ItemAttributes = new List<ItemAttribute>(attributes)
        };

        private static ItemAttribute Attribute(AttributesType type) => new()
        {
            attrType = type,
            modifiers = new List<ItemModifier>()
        };

        private static ItemAttribute Attribute(AttributesType type, ModifierType modifierType, float value) => new()
        {
            attrType = type,
            modifiers = new List<ItemModifier> { Modifier(modifierType, value) }
        };

        private static ItemModifier Modifier(ModifierType type, float value) => new()
        {
            modifierType = type,
            value = value
        };

        private sealed class FakeUseTarget : IItemUseTarget
        {
            public float Health { get; private set; }
            public float Oxygen { get; private set; }
            public float Food { get; private set; }
            public float Water { get; private set; }

            public void RestoreHealth(float amount) => Health += amount;
            public void ModifyOxygen(float amount) => Oxygen += amount;
            public void ModifyFood(float amount) => Food += amount;
            public void ModifyWater(float amount) => Water += amount;
        }
    }
}
#endif
