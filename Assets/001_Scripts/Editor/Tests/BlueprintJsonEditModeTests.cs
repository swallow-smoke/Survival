#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using _001_Scripts.Controller;
using _001_Scripts.Data.Item;
using _001_Scripts.Data.Message;
using _001_Scripts.Data.SOJ;
using _001_Scripts.Interface;
using MessagePipe;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace _001_Scripts.Editor.Tests
{
    public sealed class BlueprintJsonEditModeTests
    {
        [Test]
        public void Craft_RemovesConfiguredIngredientCountExactlyOnce()
        {
            var database = ScriptableObject.CreateInstance<BluePrintDataBase>();
            var host = new GameObject("CraftController Test");
            try
            {
                database.LoadJson("{\"blueprints\":[{\"resultCraft\":2,\"recipe\":[{\"item\":0,\"count\":2}],\"isUnlocked\":true,\"bluePrintName\":\"Iron Test\",\"bluePrintId\":1}]}");
                var inventory = new RecordingInventory(0, 10);
                var controller = host.AddComponent<CraftController>();
                SetPrivate(controller, "bpDB", database);
                SetPrivate(controller, "_invServ", inventory);
                SetPrivate(controller, "_inventoryWriter", inventory);
                SetPrivate(controller, "_craftResultMessagePublisher", new RecordingPublisher<CraftResultMessage>());

                controller.Craft("Iron Test");

                Assert.That(inventory.Count(0), Is.EqualTo(8));
                Assert.That(inventory.RemoveCalls, Is.EqualTo(1));
                Assert.That(inventory.Count(2), Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
                UnityEngine.Object.DestroyImmediate(database);
            }
        }

        [Test]
        public void LoadJson_ParsesRecipeAndNormalizesCategoryPath()
        {
            var database = ScriptableObject.CreateInstance<BluePrintDataBase>();
            try
            {
                database.LoadJson("{\"blueprints\":[{\"resultCraft\":2,\"recipe\":[{\"item\":0,\"count\":2}],\"unlockProgress\":1,\"unlockRequired\":3,\"categoryPath\":\" Materials / Metal / Iron \",\"bluePrintName\":\"Filter\",\"bluePrintId\":4}]}");

                var blueprint = database.GetBluePrint(4);
                Assert.That(blueprint, Is.Not.Null);
                Assert.That(blueprint.categoryPath, Is.EqualTo("Materials/Metal/Iron"));
                Assert.That(blueprint.recipe, Has.Count.EqualTo(1));
                Assert.That(blueprint.recipe[0].item, Is.Zero);
                Assert.That(blueprint.recipe[0].count, Is.EqualTo(2));
                Assert.That(blueprint.unlockProgress, Is.EqualTo(1));
                Assert.That(blueprint.unlockRequired, Is.EqualTo(3));
                Assert.That(blueprint.isUnlocked, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(database);
            }
        }

        [Test]
        public void LoadJson_RejectsDuplicateBlueprintIds()
        {
            var database = ScriptableObject.CreateInstance<BluePrintDataBase>();
            try
            {
                const string json = "{\"blueprints\":[{\"bluePrintName\":\"A\",\"bluePrintId\":1},{\"bluePrintName\":\"B\",\"bluePrintId\":1}]}";
                Assert.Throws<FormatException>(() => database.LoadJson(json));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(database);
            }
        }

        [Test]
        public void ProjectTestContent_HasCraftableChainAndMatchingItems()
        {
            TextAsset json = Resources.Load<TextAsset>("Data/Blueprints");
            ItemDataBase items = AssetDatabase.LoadAssetAtPath<ItemDataBase>(
                "Assets/003_Resources/Data/ItemDataBase.asset");
            Assert.That(json, Is.Not.Null);
            Assert.That(items, Is.Not.Null);

            var database = ScriptableObject.CreateInstance<BluePrintDataBase>();
            try
            {
                database.LoadJson(json.text);
                int[] blueprintIds = { 10, 11, 12, 13, 14, 15, 16 };
                int[] itemIds = { 8, 9, 10, 11, 12, 13, 14, 15, 16, 17 };
                foreach (int id in blueprintIds)
                    Assert.That(database.GetBluePrint(id), Is.Not.Null, $"Missing blueprint {id}");
                foreach (int id in itemIds)
                    Assert.That(items.GetAllItems().ContainsKey(id), Is.True, $"Missing item {id}");

                Assert.That(database.GetBluePrint(13).resultCraft, Is.EqualTo(14));
                Assert.That(database.GetBluePrint(15).unlockProgress, Is.EqualTo(2));
                Assert.That(database.GetBluePrint(15).unlockRequired, Is.EqualTo(3));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(database);
            }
        }

        private static void SetPrivate(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {fieldName}");
            field.SetValue(target, value);
        }

        private sealed class RecordingPublisher<T> : IPublisher<T>
        {
            public void Publish(T message) { }
        }

        private sealed class RecordingInventory : IInventoryReader, IInventoryWriter
        {
            private readonly Dictionary<int, int> _counts = new();
            public int RemoveCalls { get; private set; }
            public int SlotCount => 0;

            public RecordingInventory(int itemId, int count) => _counts[itemId] = count;
            public int Count(int itemId) => _counts.TryGetValue(itemId, out int count) ? count : 0;
            public bool HasItem(int id, int count = 1) => Count(id) >= count;
            public bool HasItem(Item item, int count = 1) => HasItem(item.itemId, count);
            public bool HasItem(Instance ins) => HasItem(ins.itemId);
            public IReadOnlyList<InventorySlot> GetAllItems() => Array.Empty<InventorySlot>();
            public InventorySlot GetSlot(int index) => throw new IndexOutOfRangeException();

            public AddItemResult AddItem(int id, int count)
            {
                _counts[id] = Count(id) + count;
                return new AddItemResult(0, new List<int>());
            }

            public void RemoveItem(int id, int count)
            {
                RemoveCalls++;
                _counts[id] = Count(id) - count;
            }

            public void RemoveItem(Item item) => RemoveItem(item.itemId, Count(item.itemId));
            public void RemoveItem(Instance ins) => RemoveItem(ins.itemId, 1);
        }
    }
}
#endif
