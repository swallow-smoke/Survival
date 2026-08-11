#if UNITY_EDITOR
using _001_Scripts.UI.Component;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace _001_Scripts.Editor.Tests
{
    public sealed class CrosshairGraphicEditModeTests
    {
        [Test]
        public void InteractionMode_UsesDifferentGeneratedGeometry()
        {
            var gameObject = new GameObject("Crosshair Test", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(CrosshairGraphic));
            try
            {
                gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(64f, 64f);
                var graphic = gameObject.GetComponent<CrosshairGraphic>();
                graphic.Rebuild(CanvasUpdate.PreRender);
                int defaultVertices = graphic.canvasRenderer.GetMesh().vertexCount;

                graphic.Interaction = true;
                graphic.Rebuild(CanvasUpdate.PreRender);
                int interactionVertices = graphic.canvasRenderer.GetMesh().vertexCount;

                Assert.That(defaultVertices, Is.GreaterThan(0));
                Assert.That(interactionVertices, Is.GreaterThan(0));
                Assert.That(interactionVertices, Is.Not.EqualTo(defaultVertices));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }
    }
}
#endif
