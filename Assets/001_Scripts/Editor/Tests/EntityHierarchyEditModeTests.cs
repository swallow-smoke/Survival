using System.Linq;
using System.Reflection;
using _001_Scripts.Controller;
using _001_Scripts.Data.Structure.Interface;
using _001_Scripts.Entities;
using _001_Scripts.Structure;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Serialization;

namespace _001_Scripts.Editor.Tests
{
    public sealed class EntityHierarchyEditModeTests
    {
        [Test]
        public void Entity_AutomaticallyFindsChildFeatures()
        {
            var root = new GameObject("EntityRoot");
            var child = new GameObject("FeatureChild");
            try
            {
                var entity = root.AddComponent<Entity>();
                child.transform.SetParent(root.transform);
                var health = child.AddComponent<Health>();

                Assert.That(entity.TryGetFeature<Health>(out var resolved), Is.True);
                Assert.That(resolved, Is.SameAs(health));
                Assert.That(health.Owner, Is.SameAs(entity));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void NestedEntity_IsAFeatureBoundary()
        {
            var root = new GameObject("Root");
            var nested = new GameObject("Nested");
            try
            {
                var rootEntity = root.AddComponent<Entity>();
                nested.transform.SetParent(root.transform);
                var nestedEntity = nested.AddComponent<Entity>();
                var nestedHealth = nested.AddComponent<Health>();

                Assert.That(rootEntity.TryGetFeature<Health>(out _), Is.False);
                Assert.That(nestedEntity.TryGetFeature<Health>(out var resolved), Is.True);
                Assert.That(resolved, Is.SameAs(nestedHealth));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Fabricator_IsAnAutomaticallyRegisteredInteractionFeature()
        {
            var go = new GameObject("Fabricator");
            try
            {
                var entity = go.AddComponent<Entity>();
                var fabricator = go.AddComponent<Fabricator>();

                Assert.That(entity.TryGetFeature<IInteractionTarget>(out var target), Is.True);
                Assert.That(target, Is.SameAs(fabricator));
                Assert.That(fabricator, Is.InstanceOf<MonoBehaviour>());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void GameplayControllers_NoLongerInheritEntityTypes()
        {
            Assert.That(typeof(PlayerController).BaseType, Is.EqualTo(typeof(MonoBehaviour)));
            Assert.That(typeof(SmallSubVehicle).BaseType, Is.EqualTo(typeof(MonoBehaviour)));
            Assert.That(typeof(LargeSubVehicle).BaseType, Is.EqualTo(typeof(MonoBehaviour)));
        }

        [Test]
        public void DataFeatures_PreserveLegacyFieldAliases()
        {
            AssertFormerName(typeof(Health), "maxHealth", "maxHP");
            AssertFormerName(typeof(WorldItem), "itemId", "dropItemId");
            AssertFormerName(typeof(WorldItem), "count", "dropCount");
        }

        private static void AssertFormerName(System.Type declaringType, string fieldName, string oldName)
        {
            var field = declaringType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            var names = field.GetCustomAttributes<FormerlySerializedAsAttribute>().Select(attribute => attribute.oldName);
            Assert.That(names, Does.Contain(oldName));
        }
    }
}
