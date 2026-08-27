using AstraNope.Core.World.Water.Interfaces;
using UnityEngine;
using VContainer;

namespace AstraNope.Core.World.Water
{
    [AddComponentMenu("Survival/Water/Water Debug Probe")]
    public sealed class WaterDebugProbe : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private bool drawOverlay = true;
        [SerializeField] private Vector2 overlayPosition = new Vector2(12f, 12f);

        private IWaterQueryService _query;
        private WaterSample _sample;
        private bool _hasSample;

        [Inject]
        public void Construct(IWaterQueryService query) => _query = query;

        private void OnEnable()
        {
            if (_query == null) _query = WaterRegistryLocator.Current as IWaterQueryService;
        }

        private void Update()
        {
            if (_query == null) return;
            _hasSample = _query.TrySample(target != null ? target.position : transform.position, out _sample);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void DrawRuntimeOverlay()
        {
            if (!drawOverlay) return;
            Rect area = new Rect(overlayPosition.x, overlayPosition.y, 360f, 120f);
            string text = !_hasSample
                ? "Water: none"
                : $"Water: {_sample.WaterBody}\nType: {_sample.BodyType}\nSurface: {_sample.SurfaceHeight:F2}  Signed depth: {_sample.SignedDepth:F2}\nFlow: {_sample.FlowVelocity}";
            GUI.Box(area, text);
        }

        private void OnGUI() => DrawRuntimeOverlay();
    }
}
