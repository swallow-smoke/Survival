#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using _001_Scripts.UI.Component;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _001_Scripts.Editor.Tests
{
    public sealed class SimpleRadialMenuEditModeTests
    {
        [Test]
        public void NotificationCapacityTrim_RemovesOldestWithoutDeferredDestroyLoop()
        {
            var host = new GameObject("NotificationFeedTest", typeof(RectTransform));
            try
            {
                var feed = host.AddComponent<LeftNotificationFeed>();
                feed.EnsureView();
                Transform notificationRoot = host.transform.Find("LeftNotificationFeed");
                for (int i = 0; i < 6; i++)
                    new GameObject($"Notification_{i}", typeof(RectTransform)).transform.SetParent(notificationRoot, false);

                MethodInfo trim = typeof(LeftNotificationFeed).GetMethod("TrimForIncomingNotification",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(trim, Is.Not.Null);
                trim.Invoke(feed, null);

                Assert.That(notificationRoot.childCount, Is.EqualTo(5),
                    "The oldest entry must leave the hierarchy immediately before deferred destruction.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

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
                Assert.That(nodes.childCount, Is.EqualTo(24));
                int activeNodes = 0;
                for (int i = 0; i < nodes.childCount; i++)
                {
                    Assert.That(nodes.GetChild(i).GetComponent<RadialCircleGraphic>(), Is.Not.Null);
                    Assert.That(nodes.GetChild(i).GetComponent<Button>(), Is.Not.Null);
                    Assert.That(nodes.GetChild(i).GetComponent<Outline>(), Is.Not.Null);
                    Assert.That(nodes.GetChild(i).GetComponent<SimpleRadialNodeView>(), Is.Not.Null);
                    if (nodes.GetChild(i).gameObject.activeSelf) activeNodes++;
                }
                Assert.That(activeNodes, Is.EqualTo(3));

                for (int pass = 0; pass < 100; pass++)
                    view.SetEntries(new List<SimpleRadialEntry>
                    {
                        new("One", "○", "One", () => { }),
                        new("Two", "○", "Two", () => { })
                    });
                Assert.That(nodes.childCount, Is.EqualTo(24), "Refreshing must reuse the fixed node pool.");

                view.SetPinnedRecipe("Pinned", "Iron  2 / 3   부족 1");
                Transform pinned = root.transform.Find("SimpleRadialRoot/PinnedRecipe");
                Assert.That(pinned.gameObject.activeSelf, Is.True);
                Assert.That(pinned.Find("Body").GetComponent<Text>().text, Does.Contain("부족 1"));

                bool rightClicked = false;
                view.SetEntries(new List<SimpleRadialEntry>
                {
                    new("Recipe", "○", "Recipe", () => { }, false, "tooltip", () => rightClicked = true)
                });
                var rightClick = new PointerEventData(null) { button = PointerEventData.InputButton.Right };
                nodes.GetChild(0).GetComponent<SimpleRadialNodeView>().OnPointerClick(rightClick);
                Assert.That(rightClicked, Is.True, "Right-click pinning must work even when materials are missing.");

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
