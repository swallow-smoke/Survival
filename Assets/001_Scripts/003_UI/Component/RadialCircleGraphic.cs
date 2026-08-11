using UnityEngine;
using UnityEngine.UI;

namespace _001_Scripts.UI.Component
{
    [AddComponentMenu("UI/Survival/Radial Circle Graphic")]
    public sealed class RadialCircleGraphic : MaskableGraphic, ICanvasRaycastFilter
    {
        [SerializeField, Range(12, 96)] private int segments = 48;
        [SerializeField, Range(0f, .95f)] private float innerRadius;

        public float InnerRadius
        {
            get => innerRadius;
            set
            {
                innerRadius = Mathf.Clamp(value, 0f, .95f);
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            Rect rect = GetPixelAdjustedRect();
            Vector2 center = rect.center;
            float radius = Mathf.Min(rect.width, rect.height) * .5f;
            int count = Mathf.Max(12, segments);

            if (innerRadius <= .001f)
            {
                AddVertex(vertexHelper, center);
                for (int i = 0; i <= count; i++) AddVertex(vertexHelper, center + Direction(i, count) * radius);
                for (int i = 0; i < count; i++) vertexHelper.AddTriangle(0, i + 1, i + 2);
                return;
            }

            float inner = radius * innerRadius;
            for (int i = 0; i <= count; i++)
            {
                Vector2 direction = Direction(i, count);
                AddVertex(vertexHelper, center + direction * radius);
                AddVertex(vertexHelper, center + direction * inner);
            }

            for (int i = 0; i < count; i++)
            {
                int outer = i * 2;
                int innerIndex = outer + 1;
                int nextOuter = outer + 2;
                int nextInner = outer + 3;
                vertexHelper.AddTriangle(outer, nextOuter, nextInner);
                vertexHelper.AddTriangle(outer, nextInner, innerIndex);
            }
        }

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera,
                    out Vector2 localPoint)) return false;
            Rect rect = rectTransform.rect;
            float radius = Mathf.Min(rect.width, rect.height) * .5f;
            return (localPoint - rect.center).sqrMagnitude <= radius * radius;
        }

        private void AddVertex(VertexHelper helper, Vector2 position)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;
            vertex.position = position;
            helper.AddVert(vertex);
        }

        private static Vector2 Direction(int index, int count)
        {
            float angle = index / (float)count * Mathf.PI * 2f;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }
    }
}
