using UnityEngine;
using UnityEngine.UI;

namespace _001_Scripts.UI.Component
{
    [AddComponentMenu("UI/Survival/Right Rounded Rectangle")]
    public sealed class RightRoundedRectGraphic : MaskableGraphic
    {
        [SerializeField, Min(0f)] private float radius = 18f;
        [SerializeField, Range(2, 16)] private int cornerSegments = 6;

        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            Rect rect = GetPixelAdjustedRect();
            float roundedRadius = Mathf.Min(radius, Mathf.Min(rect.width, rect.height) * .5f);

            AddVertex(helper, rect.center);
            AddVertex(helper, new Vector2(rect.xMin, rect.yMin));
            AddVertex(helper, new Vector2(rect.xMax - roundedRadius, rect.yMin));
            AddArc(helper, new Vector2(rect.xMax - roundedRadius, rect.yMin + roundedRadius),
                roundedRadius, -90f, 0f);
            AddArc(helper, new Vector2(rect.xMax - roundedRadius, rect.yMax - roundedRadius),
                roundedRadius, 0f, 90f);
            AddVertex(helper, new Vector2(rect.xMin, rect.yMax));

            int perimeterCount = helper.currentVertCount - 1;
            for (int i = 0; i < perimeterCount; i++)
                helper.AddTriangle(0, i + 1, i + 1 < perimeterCount ? i + 2 : 1);
        }

        private void AddArc(VertexHelper helper, Vector2 center, float arcRadius, float start, float end)
        {
            for (int i = 1; i <= cornerSegments; i++)
            {
                float angle = Mathf.Lerp(start, end, i / (float)cornerSegments) * Mathf.Deg2Rad;
                AddVertex(helper, center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * arcRadius);
            }
        }

        private void AddVertex(VertexHelper helper, Vector2 position)
        {
            var vertex = UIVertex.simpleVert;
            vertex.color = color;
            vertex.position = position;
            helper.AddVert(vertex);
        }
    }
}
