#if UNITY_EDITOR
using System;
using _001_Scripts.Data.SOJ;
using NUnit.Framework;
using UnityEngine;

namespace _001_Scripts.Editor.Tests
{
    public sealed class BlueprintJsonEditModeTests
    {
        [Test]
        public void LoadJson_ParsesRecipeAndNormalizesCategoryPath()
        {
            var database = ScriptableObject.CreateInstance<BluePrintDataBase>();
            try
            {
                database.LoadJson("{\"blueprints\":[{\"resultCraft\":2,\"recipe\":[{\"item\":0,\"count\":2}],\"categoryPath\":\" Materials / Metal / Iron \",\"bluePrintName\":\"Filter\",\"bluePrintId\":4}]}");

                var blueprint = database.GetBluePrint(4);
                Assert.That(blueprint, Is.Not.Null);
                Assert.That(blueprint.categoryPath, Is.EqualTo("Materials/Metal/Iron"));
                Assert.That(blueprint.recipe, Has.Count.EqualTo(1));
                Assert.That(blueprint.recipe[0].item, Is.Zero);
                Assert.That(blueprint.recipe[0].count, Is.EqualTo(2));
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
    }
}
#endif
