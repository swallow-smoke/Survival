#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using _001_Scripts.Controller;
using _001_Scripts.Data.Item;
using _001_Scripts.Data.Message;
using NUnit.Framework;
using UnityEngine;

namespace _001_Scripts.Editor.Tests
{
    public sealed class HotbarTransferEditModeTests
    {
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
                    new(new Instance(5, 10, 100f), 1),
                    new(null, 0)
                });
                SetField(controller, "hotbarItems", new List<InventorySlot>
                {
                    new(null, 0),
                    new(null, 0)
                });

                InvokeSwap(controller, new InvSwapMessage(0, 0,
                    InventorySlotArea.Inventory, InventorySlotArea.Hotbar));
                Assert.That(controller.GetSlot(0).IsEmpty, Is.True);
                Assert.That(controller.GetHotbarSlot(0).ins.itemId, Is.EqualTo(5));

                InvokeSwap(controller, new InvSwapMessage(0, 1,
                    InventorySlotArea.Hotbar, InventorySlotArea.Inventory));
                Assert.That(controller.GetHotbarSlot(0).IsEmpty, Is.True);
                Assert.That(controller.GetSlot(1).ins.itemId, Is.EqualTo(5));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        private static void InvokeSwap(InventoryController controller, InvSwapMessage message) =>
            typeof(InventoryController).GetMethod("SwapItem", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(controller, new object[] { message });

        private static void SetField<T>(InventoryController controller, string name, T value) =>
            typeof(InventoryController).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(controller, value);
    }
}
#endif
