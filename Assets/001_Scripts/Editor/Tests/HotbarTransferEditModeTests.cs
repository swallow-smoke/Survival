#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using AstraNope.Gameplay.Player;
using AstraNope.Data.Items;
using AstraNope.Data.Messages;
using AstraNope.Data.Databases;
using NUnit.Framework;
using UnityEngine;

namespace AstraNope.Editor.Tests
{
    public sealed class HotbarTransferEditModeTests
    {
        [Test]
        public void NormalizeSlots_ClearsZeroStackInstancesRestoredByUnitySerialization()
        {
            var gameObject = new GameObject("Inventory Serialization Test");
            try
            {
                var controller = gameObject.AddComponent<InventoryController>();
                SetField(controller, "maxSlots", 2);
                SetField(controller, "hotbarSlotCount", 1);
                SetField(controller, "items", new List<InventorySlot>
                {
                    new(new ItemInstance(0, 0, 0f), 0),
                    new(new ItemInstance(5, 10, 100f), 1)
                });
                SetField(controller, "hotbarItems", new List<InventorySlot>
                {
                    new(new ItemInstance(0, 0, 0f), 0)
                });

                controller.GetAllItems();

                Assert.That(controller.GetSlot(0).IsEmpty, Is.True);
                Assert.That(controller.GetSlot(0).ins, Is.Null,
                    "A zero-stack default instance must not become item id 0.");
                Assert.That(controller.GetSlot(1).ins.itemId, Is.EqualTo(5));
                Assert.That(controller.GetHotbarSlot(0).ins, Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void SwapItem_MovesBetweenInventoryAndHotbarInBothDirections()
        {
            var gameObject = new GameObject("InventoryController Test");
            try
            {
                var controller = gameObject.AddComponent<InventoryController>();
                SetField(controller, "maxSlots", 2);
                SetField(controller, "hotbarSlotCount", 2);
                SetField(controller, "items", new List<InventorySlot>
                {
                    new(new ItemInstance(5, 10, 100f), 1),
                    new(null, 0)
                });
                SetField(controller, "hotbarItems", new List<InventorySlot>
                {
                    new(null, 0),
                    new(null, 0)
                });

                InvokeSwap(controller, new InventorySwapMessage(0, 0,
                    InventorySlotArea.Inventory, InventorySlotArea.Hotbar));
                Assert.That(controller.GetSlot(0).IsEmpty, Is.True);
                Assert.That(controller.GetHotbarSlot(0).ins.itemId, Is.EqualTo(5));

                InvokeSwap(controller, new InventorySwapMessage(0, 0,
                    InventorySlotArea.Hotbar, InventorySlotArea.Inventory));
                Assert.That(controller.GetHotbarSlot(0).IsEmpty, Is.True);
                Assert.That(controller.GetSlot(0).ins.itemId, Is.EqualTo(5));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void SwapItem_EquipmentAcceptsOnlyMatchingSlotAndCanReturnToInventory()
        {
            var gameObject = new GameObject("Equipment Transfer Test");
            var database = ScriptableObject.CreateInstance<ItemDataBase>();
            try
            {
                database.itemList.Add(100, new Item
                {
                    itemId = 100,
                    itemName = "Test Body",
                    equipmentSlot = EquipmentSlotType.Body
                });
                var controller = gameObject.AddComponent<InventoryController>();
                SetField(controller, "itemDB", database);
                SetField(controller, "maxSlots", 2);
                SetField(controller, "hotbarSlotCount", 1);
                SetField(controller, "items", new List<InventorySlot>
                {
                    new(new ItemInstance(100, 1, 50f), 1),
                    new(null, 0)
                });

                InvokeSwap(controller, new InventorySwapMessage(0, 0,
                    InventorySlotArea.Inventory, InventorySlotArea.Equipment));
                Assert.That(controller.GetSlot(0).ins.itemId, Is.EqualTo(100),
                    "Body equipment must be rejected by the head slot.");

                InvokeSwap(controller, new InventorySwapMessage(0, 1,
                    InventorySlotArea.Inventory, InventorySlotArea.Equipment));
                Assert.That(controller.GetSlot(0).IsEmpty, Is.True);
                Assert.That(controller.GetEquipmentSlot(1).ins.itemId, Is.EqualTo(100));

                InvokeSwap(controller, new InventorySwapMessage(1, 0,
                    InventorySlotArea.Equipment, InventorySlotArea.Inventory));
                Assert.That(controller.GetEquipmentSlot(1).IsEmpty, Is.True);
                Assert.That(controller.GetSlot(0).ins.itemId, Is.EqualTo(100));

                InvokeSwap(controller, new InventorySwapMessage(0, 0,
                    InventorySlotArea.Inventory, InventorySlotArea.Hotbar));
                Assert.That(controller.GetSlot(0).ins.itemId, Is.EqualTo(100),
                    "Wearable equipment must not move into the hotbar.");
                Assert.That(controller.GetHotbarSlot(0).IsEmpty, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
                UnityEngine.Object.DestroyImmediate(database);
            }
        }

        private static void InvokeSwap(InventoryController controller, InventorySwapMessage message) =>
            typeof(InventoryController).GetMethod("SwapItem", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(controller, new object[] { message });

        private static void SetField<T>(InventoryController controller, string name, T value) =>
            typeof(InventoryController).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(controller, value);
    }
}
#endif
