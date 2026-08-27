using UnityEngine;
using UnityEngine.UI;

namespace AstraNope.UI.Components
{
    [AddComponentMenu("UI/Survival/Crosshair Graphic")]
    public sealed class CrosshairGraphic : MaskableGraphic
    {
        [SerializeField] private bool interaction;
        [SerializeField, Range(-1f, 1f)] private float progress = -1f;

        public bool Interaction
        {
            get => interaction;
            set
            {
                if (interaction == value) return;
                interaction = value;
                SetVerticesDirty();
            }
        }

        public float Progress
        {
            get => progress;
            set
            {
                float next = value < 0f ? -1f : Mathf.Clamp01(value);
                if (Mathf.Approximately(progress, next)) return;
                progress = next;
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            Vector2 center = GetPixelAdjustedRect().center;
            if (interaction) DrawInteraction(helper, center);
            else DrawDefault(helper, center);
            if (progress >= 0f) DrawProgress(helper, center);
        }

        private void DrawProgress(VertexHelper helper, Vector2 center)
        {
            Color background = new(color.r, color.g, color.b, color.a * .2f);
            AddArc(helper, center, 27f, 3.2f, 1f, -90f, background);
            if (progress > .001f)
                AddArc(helper, center, 27f, 3.2f, progress, -90f, color);
        }

        private void DrawDefault(VertexHelper helper, Vector2 center)
        {
            AddCircle(helper, center, 2.1f, 20);
            AddRing(helper, center, 11f, 1.15f, 56);
            const float inner = 15f;
            const float outer = 19f;
            const float thickness = 1.1f;
            AddQuad(helper, center + new Vector2(-thickness * .5f, inner),
                center + new Vector2(thickness * .5f, outer));
            AddQuad(helper, center + new Vector2(-thickness * .5f, -outer),
                center + new Vector2(thickness * .5f, -inner));
            AddQuad(helper, center + new Vector2(inner, -thickness * .5f),
                center + new Vector2(outer, thickness * .5f));
            AddQuad(helper, center + new Vector2(-outer, -thickness * .5f),
                center + new Vector2(-inner, thickness * .5f));
        }

        private void DrawInteraction(VertexHelper helper, Vector2 center)
        {
            const float outer = 18f;
            const float inner = 8f;
            const float thickness = 2f;
            AddBracket(helper, center, -1f, 1f, inner, outer, thickness);
            AddBracket(helper, center, 1f, 1f, inner, outer, thickness);
            AddBracket(helper, center, -1f, -1f, inner, outer, thickness);
            AddBracket(helper, center, 1f, -1f, inner, outer, thickness);
            AddDiamond(helper, center, 3f);
        }

        private void AddBracket(VertexHelper helper, Vector2 center, float xSign, float ySign,
            float inner, float outer, float thickness)
        {
            float xOuter = center.x + xSign * outer;
            float yOuter = center.y + ySign * outer;
            float xInner = center.x + xSign * inner;
            float yInner = center.y + ySign * inner;
            AddQuad(helper,
                new Vector2(Mathf.Min(xOuter, xInner), yOuter - thickness * .5f),
                new Vector2(Mathf.Max(xOuter, xInner), yOuter + thickness * .5f));
            AddQuad(helper,
                new Vector2(xOuter - thickness * .5f, Mathf.Min(yOuter, yInner)),
                new Vector2(xOuter + thickness * .5f, Mathf.Max(yOuter, yInner)));
        }

        private void AddDiamond(VertexHelper helper, Vector2 center, float radius)
        {
            int start = helper.currentVertCount;
            AddVertex(helper, center + Vector2.up * radius);
            AddVertex(helper, center + Vector2.right * radius);
            AddVertex(helper, center + Vector2.down * radius);
            AddVertex(helper, center + Vector2.left * radius);
            helper.AddTriangle(start, start + 1, start + 2);
            helper.AddTriangle(start, start + 2, start + 3);
        }

        private void AddCircle(VertexHelper helper, Vector2 center, float radius, int segments)
        {
            int centerIndex = helper.currentVertCount;
            AddVertex(helper, center);
            for (int i = 0; i <= segments; i++)
            {
                float angle = i / (float)segments * Mathf.PI * 2f;
                AddVertex(helper, center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }
            for (int i = 0; i < segments; i++)
                helper.AddTriangle(centerIndex, centerIndex + i + 1, centerIndex + i + 2);
        }

        private void AddRing(VertexHelper helper, Vector2 center, float radius, float thickness, int segments)
        {
            float inner = radius - thickness * .5f;
            float outer = radius + thickness * .5f;
            int start = helper.currentVertCount;
            for (int i = 0; i <= segments; i++)
            {
                float angle = i / (float)segments * Mathf.PI * 2f;
                Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));
                AddVertex(helper, center + direction * outer);
                AddVertex(helper, center + direction * inner);
            }
            for (int i = 0; i < segments; i++)
            {
                int current = start + i * 2;
                helper.AddTriangle(current, current + 2, current + 3);
                helper.AddTriangle(current, current + 3, current + 1);
            }
        }

        private void AddArc(VertexHelper helper, Vector2 center, float radius, float thickness,
            float normalized, float startDegrees, Color vertexColor)
        {
            int segments = Mathf.Max(1, Mathf.CeilToInt(64f * Mathf.Clamp01(normalized)));
            float inner = radius - thickness * .5f;
            float outer = radius + thickness * .5f;
            int start = helper.currentVertCount;
            float arc = Mathf.PI * 2f * Mathf.Clamp01(normalized);
            float startRadians = startDegrees * Mathf.Deg2Rad;
            for (int i = 0; i <= segments; i++)
            {
                float angle = startRadians + arc * (i / (float)segments);
                Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));
                AddVertex(helper, center + direction * outer, vertexColor);
                AddVertex(helper, center + direction * inner, vertexColor);
            }
            for (int i = 0; i < segments; i++)
            {
                int current = start + i * 2;
                helper.AddTriangle(current, current + 2, current + 3);
                helper.AddTriangle(current, current + 3, current + 1);
            }
        }

        private void AddQuad(VertexHelper helper, Vector2 min, Vector2 max)
        {
            int start = helper.currentVertCount;
            AddVertex(helper, new Vector2(min.x, min.y));
            AddVertex(helper, new Vector2(max.x, min.y));
            AddVertex(helper, new Vector2(max.x, max.y));
            AddVertex(helper, new Vector2(min.x, max.y));
            helper.AddTriangle(start, start + 1, start + 2);
            helper.AddTriangle(start, start + 2, start + 3);
        }

        private void AddVertex(VertexHelper helper, Vector2 position)
            => AddVertex(helper, position, color);

        private void AddVertex(VertexHelper helper, Vector2 position, Color vertexColor)
        {
            var vertex = UIVertex.simpleVert;
            vertex.color = vertexColor;
            vertex.position = position;
            helper.AddVert(vertex);
        }
    }
}
