using System;
using AstraNope.Core.World.Water.Interfaces;
using UnityEngine;

namespace AstraNope.Core.World.Water
{
    [AddComponentMenu("Survival/Water/Player Water Sensor")]
    public sealed class PlayerWaterSensor : MonoBehaviour
    {
        [Header("Sample Points")]
        [SerializeField] private Transform feet;
        [SerializeField] private Transform chest;
        [SerializeField] private Transform head;
        [SerializeField] private Transform cameraPoint;
        [SerializeField] private Vector3 fallbackFeetOffset = new Vector3(0f, 0.1f, 0f);
        [SerializeField] private Vector3 fallbackChestOffset = new Vector3(0f, 1.1f, 0f);
        [SerializeField] private Vector3 fallbackHeadOffset = new Vector3(0f, 1.75f, 0f);

        [Header("Hysteresis")]
        [Min(0f), SerializeField] private float enterDepth = 0.08f;
        [Min(0f), SerializeField] private float exitDepth = 0.02f;

        private IWaterQueryService _query;
        public PlayerWaterState Current { get; private set; }
        public event Action<PlayerWaterState> StateChanged;

        public void Configure(IWaterQueryService queryService, Transform feetPoint,
            Transform chestPoint, Transform headPoint, Transform cameraSamplePoint)
        {
            _query = queryService;
            if (feetPoint != null) feet = feetPoint;
            if (chestPoint != null) chest = chestPoint;
            if (headPoint != null) head = headPoint;
            if (cameraSamplePoint != null) cameraPoint = cameraSamplePoint;
        }

        public PlayerWaterState SampleNow()
        {
            if (_query == null) return Current;

            bool feetValid = _query.TrySample(GetPosition(feet, fallbackFeetOffset), out WaterSample feetSample);
            bool chestValid = _query.TrySample(GetPosition(chest, fallbackChestOffset), out WaterSample chestSample);
            bool headValid = _query.TrySample(GetPosition(head, fallbackHeadOffset), out WaterSample headSample);
            Vector3 cameraPosition = cameraPoint != null ? cameraPoint.position : GetPosition(head, fallbackHeadOffset);
            bool cameraValid = _query.TrySample(cameraPosition, out WaterSample cameraSample);

            bool touching = IsSubmerged(feetValid, feetSample, Current.TouchingWater);
            bool swimming = IsSubmerged(chestValid, chestSample, Current.Swimming);
            bool headUnderwater = IsSubmerged(headValid, headSample, Current.HeadUnderwater);
            bool cameraUnderwater = IsSubmerged(cameraValid, cameraSample, Current.CameraUnderwater);
            bool wading = touching && !swimming;
            IWaterBody body = swimming ? chestSample.WaterBody : touching ? feetSample.WaterBody :
                headUnderwater ? headSample.WaterBody : cameraUnderwater ? cameraSample.WaterBody : null;

            PlayerWaterState next = new PlayerWaterState(touching, wading, swimming,
                headUnderwater, cameraUnderwater, body, chestSample, cameraSample);
            if (!Current.Equals(next)) StateChanged?.Invoke(next);
            Current = next;
            return next;
        }

        private bool IsSubmerged(bool valid, WaterSample sample, bool wasSubmerged)
        {
            if (!valid) return false;
            return sample.SignedDepth > (wasSubmerged ? exitDepth : enterDepth);
        }

        private Vector3 GetPosition(Transform point, Vector3 fallbackOffset) =>
            point != null ? point.position : transform.TransformPoint(fallbackOffset);

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            DrawPoint(GetPosition(feet, fallbackFeetOffset), Color.cyan);
            DrawPoint(GetPosition(chest, fallbackChestOffset), Color.blue);
            DrawPoint(GetPosition(head, fallbackHeadOffset), Color.magenta);
            if (cameraPoint != null) DrawPoint(cameraPoint.position, Color.yellow);
        }

        private static void DrawPoint(Vector3 position, Color color)
        {
            Gizmos.color = color;
            Gizmos.DrawWireSphere(position, 0.08f);
        }
#endif
    }
}
