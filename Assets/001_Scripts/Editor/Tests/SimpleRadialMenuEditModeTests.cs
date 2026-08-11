#if UNITY_EDITOR
using System.Collections.Generic;
using _001_Scripts.UI.Component;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace _001_Scripts.Editor.Tests
{
    public sealed class SimpleRadialMenuEditModeTests
    {
        [Test]
        public void CenterIsIconOnly_EntriesAreCircular_AndOutsideClickCloses()
        {
            var root = new GameObject("RadialTest", typeof(RectTransform));
            try
            {
                var view = root.AddComponent<SimpleRadialMenuView>();
                view.Ensure("⚙");
                bool outsideClicked = false;
                view.SetOutsideClick(() => outsideClicked = true);
                view.SetEntries(new List<SimpleRadialEntry>
                {
                    new("One", "○", "One", () => { }),
                    new("Two", "○", "Two", () => { }),
                    new("Three", "○", "Three", () => { })
                });

                Transform center = root.transform.Find("SimpleRadialRoot/Menu/Center");
                Transform nodes = root.transform.Find("SimpleRadialRoot/Menu/Nodes");
                var outside = root.transform.Find("SimpleRadialRoot/Outside").GetComponent<Button>();

                Assert.That(center, Is.Not.Null);
                Assert.That(center.GetComponent<Button>(), Is.Null);
                Assert.That(center.GetComponent<RadialCircleGraphic>().raycastTarget, Is.True);
                Assert.That(center.GetComponent<Outline>(), Is.Not.Null);
                Assert.That(nodes.childCount, Is.EqualTo(3));
                for (int i = 0; i < nodes.childCount; i++)
                {
                    Assert.That(nodes.GetChild(i).GetComponent<RadialCircleGraphic>(), Is.Not.Null);
                    Assert.That(nodes.GetChild(i).GetComponent<Button>(), Is.Not.Null);
                    Assert.That(nodes.GetChild(i).GetComponent<Outline>(), Is.Not.Null);
                }

                outside.onClick.Invoke();
                Assert.That(outsideClicked, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}
#endif
