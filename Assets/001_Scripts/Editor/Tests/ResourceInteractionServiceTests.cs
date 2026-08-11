#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using _001_Scripts.Data.Item;
using _001_Scripts.Interface;
using _001_Scripts.Managers;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using WorldBuilder.Entities.Resources;

namespace _001_Scripts.Editor.Tests
{
    public sealed class ResourceInteractionServiceTests
    {
        [Test]
        public void DroppedItem_FocusAndInteract_UsesPickupGateway()
        {
            FakeGateway gateway = new FakeGateway
            {
                Info = new ResourceInteractionInfo(new DroppedItem
                {
                    ItemId = 3,
                    Count = 2,
                    DisplayName = new FixedString64Bytes("Wood")
                })
            };
            DotsResourceInteractionService service = new DotsResourceInteractionService(gateway, new FakeSelector());

            Assert.That(service.TryFocus(Vector3.zero, Vector3.forward, 2f, out var focus), Is.True);
            Assert.That(focus.Label, Is.EqualTo("Pick up Wood"));
            Assert.That(focus.CanInteract, Is.True);
            Assert.That(service.InteractFocused(), Is.True);
            Assert.That(gateway.PickupCount, Is.EqualTo(1));
            Assert.That(gateway.HarvestCount, Is.Zero);
        }

        [Test]
        public void ResourceWithoutValidTool_IsVisibleButCannotQueueHarvest()
        {
            FakeGateway gateway = new FakeGateway { Info = Resource("Ore", HarvestMethod.Pickaxe, 1, 1f) };
            DotsResourceInteractionService service = new DotsResourceInteractionService(gateway, new FakeSelector());

            Assert.That(service.TryFocus(Vector3.zero, Vector3.forward, 2f, out var focus), Is.True);
            Assert.That(focus.CanInteract, Is.False);
            Assert.That(focus.Label, Is.EqualTo("Required harvesting tool"));
            Assert.That(service.InteractFocused(), Is.False);
            Assert.That(gateway.HarvestCount, Is.Zero);
        }

        [Test]
        public void InventorySelector_UsesSelectedHotbarTool()
        {
            HarvestToolCatalog catalog = ScriptableObject.CreateInstance<HarvestToolCatalog>();
            try
            {
                SerializedObject serialized = new SerializedObject(catalog);
                SerializedProperty tools = serialized.FindProperty("tools");
                tools.arraySize = 2;
                SetTool(tools.GetArrayElementAtIndex(0), 6, HarvestMethod.Pickaxe, 1, 1.5f, 20f);
                SetTool(tools.GetArrayElementAtIndex(1), 7, HarvestMethod.Drill, 3, 4f, 50f);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                InventoryHarvestToolSelector selector = new InventoryHarvestToolSelector(
                    new FakeHotbar(7), catalog);

                ResourceInteractionInfo info = Resource("Deposit", HarvestMethod.Pickaxe | HarvestMethod.Drill, 1, 1f);
                Assert.That(selector.TrySelect(info, out HarvestToolSelection selection), Is.True);
                Assert.That(selection.ItemId, Is.EqualTo(7));
                Assert.That(selection.Method, Is.EqualTo(HarvestMethod.Drill));
                Assert.That(selection.Power, Is.EqualTo(4f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(catalog);
            }
        }

        private static ResourceInteractionInfo Resource(string name, HarvestMethod methods, byte tier, float power)
        {
            return new ResourceInteractionInfo(new ResourceNode
            {
                DisplayName = new FixedString64Bytes(name),
                AllowedMethods = methods,
                RequiredToolItemId = -1,
                MinimumToolTier = tier,
                MinimumToolPower = power
            });
        }

        private static void SetTool(SerializedProperty property, int itemId, HarvestMethod method,
            int tier, float power, float damage)
        {
            property.FindPropertyRelative("itemId").intValue = itemId;
            property.FindPropertyRelative("method").intValue = (int)method;
            property.FindPropertyRelative("tier").intValue = tier;
            property.FindPropertyRelative("power").floatValue = power;
            property.FindPropertyRelative("damage").floatValue = damage;
        }

        private sealed class FakeSelector : IHarvestToolSelector
        {
            public bool TrySelect(ResourceInteractionInfo resource, out HarvestToolSelection selection)
            {
                selection = default;
                return false;
            }
        }

        private sealed class FakeGateway : IWorldResourceGateway
        {
            private readonly Entity target = new Entity { Index = 1, Version = 1 };
            public ResourceInteractionInfo Info;
            public int HarvestCount;
            public int PickupCount;

            public bool TryRaycast(Vector3 origin, Vector3 direction, float distance, out Entity result)
            {
                result = target;
                return true;
            }

            public bool TryGetInteractionInfo(Entity entity, out ResourceInteractionInfo info)
            {
                info = Info;
                return entity == target;
            }

            public bool TryHarvest(Entity entity, HarvestToolSelection selection)
            {
                HarvestCount++;
                return true;
            }

            public bool TryPickup(Entity entity)
            {
                PickupCount++;
                return true;
            }

            public int ProcessInventoryTransfers(Func<int, int, int> acceptItems) => 0;
        }

        private sealed class FakeHotbar : IHotbarReader
        {
            private readonly InventorySlot slot;
            public FakeHotbar(int itemId) => slot = new InventorySlot(new Instance(itemId, 1, 100f), 1);
            public int HotbarSlotCount => 1;
            public int SelectedHotbarIndex => 0;
            public InventorySlot GetHotbarSlot(int index) => slot;
        }
    }
}
#endif
