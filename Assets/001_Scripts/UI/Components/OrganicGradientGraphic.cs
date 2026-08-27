using UnityEngine;
using UnityEngine.UI;

namespace AstraNope.UI.Components
{
    [AddComponentMenu("UI/Survival/Organic Gradient")]
    public sealed class OrganicGradientGraphic : MaskableGraphic
    {
        [SerializeField] private Color topColor = new(.15f, .075f, .28f, .58f);
        [SerializeField] private Color bottomColor = new(.018f, .008f, .055f, .78f);
        [SerializeField] private Color upperFlow = new(.64f, .35f, 1f, .13f);
        [SerializeField] private Color lowerFlow = new(.23f, .48f, 1f, .09f);
        [SerializeField, Range(12, 48)] private int segments = 28;

        public void Configure(Color top, Color bottom, Color upper, Color lower)
        {
            topColor = top;
            bottomColor = bottom;
            upperFlow = upper;
            lowerFlow = lower;
            raycastTarget = false;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            Rect rect = GetPixelAdjustedRect();
            AddGradientQuad(helper, rect);
            AddFlow(helper, rect, .68f, .055f, .13f, .2f, upperFlow);
            AddFlow(helper, rect, .25f, .075f, .10f, 2.15f, lowerFlow);
            AddGlow(helper, rect, new Vector2(.10f, .20f), new Vector2(.30f, .42f), upperFlow);
            AddGlow(helper, rect, new Vector2(.90f, .76f), new Vector2(.25f, .36f), lowerFlow);
        }

        private void AddGradientQuad(VertexHelper helper, Rect rect)
        {
            int start = helper.currentVertCount;
            AddVertex(helper, new Vector2(rect.xMin, rect.yMin), bottomColor);
            AddVertex(helper, new Vector2(rect.xMin, rect.yMax), topColor);
            AddVertex(helper, new Vector2(rect.xMax, rect.yMax), topColor);
            AddVertex(helper, new Vector2(rect.xMax, rect.yMin), bottomColor);
            helper.AddTriangle(start, start + 1, start + 2);
            helper.AddTriangle(start, start + 2, start + 3);
        }

        private void AddFlow(VertexHelper helper, Rect rect, float centerY, float wave, float thickness,
            float phase, Color flowColor)
        {
            int count = Mathf.Max(12, segments);
            for (int i = 0; i < count; i++)
            {
                float t0 = i / (float)count;
                float t1 = (i + 1f) / count;
                float x0 = Mathf.Lerp(rect.xMin, rect.xMax, t0);
                float x1 = Mathf.Lerp(rect.xMin, rect.xMax, t1);
                float y0 = rect.yMin + rect.height * (centerY + Mathf.Sin(t0 * Mathf.PI * 2f + phase) * wave);
                float y1 = rect.yMin + rect.height * (centerY + Mathf.Sin(t1 * Mathf.PI * 2f + phase) * wave);
                float edge0 = Mathf.Pow(Mathf.Sin(t0 * Mathf.PI), 1.4f);
                float edge1 = Mathf.Pow(Mathf.Sin(t1 * Mathf.PI), 1.4f);
                float half0 = rect.height * thickness * edge0;
                float half1 = rect.height * thickness * edge1;
                Color center0 = WithAlpha(flowColor, flowColor.a * edge0);
                Color center1 = WithAlpha(flowColor, flowColor.a * edge1);
                Color clear = WithAlpha(flowColor, 0f);
                AddSoftSegment(helper, x0, x1, y0, y1, half0, half1, clear, center0, center1);
            }
        }

        private static void AddSoftSegment(VertexHelper helper, float x0, float x1, float y0, float y1,
            float half0, float half1, Color clear, Color center0, Color center1)
        {
            int lower = helper.currentVertCount;
            AddVertex(helper, new Vector2(x0, y0 - half0), clear);
            AddVertex(helper, new Vector2(x0, y0), center0);
            AddVertex(helper, new Vector2(x1, y1), center1);
            AddVertex(helper, new Vector2(x1, y1 - half1), clear);
            helper.AddTriangle(lower, lower + 1, lower + 2);
            helper.AddTriangle(lower, lower + 2, lower + 3);

            int upper = helper.currentVertCount;
            AddVertex(helper, new Vector2(x0, y0), center0);
            AddVertex(helper, new Vector2(x0, y0 + half0), clear);
            AddVertex(helper, new Vector2(x1, y1 + half1), clear);
            AddVertex(helper, new Vector2(x1, y1), center1);
            helper.AddTriangle(upper, upper + 1, upper + 2);
            helper.AddTriangle(upper, upper + 2, upper + 3);
        }

        private static void AddGlow(VertexHelper helper, Rect rect, Vector2 normalizedCenter,
            Vector2 normalizedSize, Color glowColor)
        {
            const int glowSegments = 30;
            Vector2 center = new(rect.xMin + rect.width * normalizedCenter.x,
                rect.yMin + rect.height * normalizedCenter.y);
            Vector2 radius = new(rect.width * normalizedSize.x * .5f, rect.height * normalizedSize.y * .5f);
            int centerIndex = helper.currentVertCount;
            AddVertex(helper, center, glowColor);
            for (int i = 0; i <= glowSegments; i++)
            {
                float angle = i / (float)glowSegments * Mathf.PI * 2f;
                AddVertex(helper, center + new Vector2(Mathf.Cos(angle) * radius.x, Mathf.Sin(angle) * radius.y),
                    WithAlpha(glowColor, 0f));
                if (i > 0) helper.AddTriangle(centerIndex, centerIndex + i, centerIndex + i + 1);
            }
        }

        private static Color WithAlpha(Color value, float alpha)
            => new(value.r, value.g, value.b, alpha);

        private static void AddVertex(VertexHelper helper, Vector2 position, Color vertexColor)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = vertexColor;
            helper.AddVert(vertex);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            raycastTarget = false;
            SetVerticesDirty();
        }
#endif
    }
}
