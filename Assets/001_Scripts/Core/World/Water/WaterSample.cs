using AstraNope.Core.World.Water.Interfaces;
using UnityEngine;

namespace AstraNope.Core.World.Water
{
    public readonly struct WaterSample
    {
        public readonly IWaterBody WaterBody;
        public readonly Vector3 QueryPosition;
        public readonly Vector3 SurfacePosition;
        public readonly Vector3 SurfaceNormal;
        public readonly float SurfaceHeight;
        public readonly float SignedDepth;
        public readonly float SubmersionDepth;
        public readonly Vector3 FlowDirection;
        public readonly Vector3 FlowVelocity;
        public readonly WaterBodyType BodyType;

        public bool IsSubmerged => SignedDepth > 0f;

        public WaterSample(IWaterBody waterBody, Vector3 queryPosition, Vector3 surfacePosition,
            Vector3 surfaceNormal, Vector3 flowVelocity, WaterBodyType bodyType)
        {
            WaterBody = waterBody;
            QueryPosition = queryPosition;
            SurfacePosition = surfacePosition;
            SurfaceNormal = surfaceNormal.sqrMagnitude > 0.000001f ? surfaceNormal.normalized : Vector3.up;
            SurfaceHeight = surfacePosition.y;
            SignedDepth = SurfaceHeight - queryPosition.y;
            SubmersionDepth = Mathf.Max(0f, SignedDepth);
            FlowVelocity = flowVelocity;
            FlowDirection = flowVelocity.sqrMagnitude > 0.000001f ? flowVelocity.normalized : Vector3.zero;
            BodyType = bodyType;
        }
    }
}
