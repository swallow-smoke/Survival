using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AstraNope.Core.World.Water
{
    [RequireComponent(typeof(Volume))]
    [AddComponentMenu("Survival/Water/Underwater Volume Controller")]
    public sealed class UnderwaterVolumeController : MonoBehaviour
    {
        [SerializeField] private UnderwaterProfile profile;
        [SerializeField] private Volume volume;
        [SerializeField] private float defaultTransitionDuration = 0.35f;

        private PlayerWaterSensor _sensor;
        private VolumeProfile _runtimeVolumeProfile;

        public bool IsCameraUnderwater => _sensor != null && _sensor.Current.CameraUnderwater;

        public void Configure(PlayerWaterSensor sensor)
        {
            _sensor = sensor;
            EnsureVolume();
        }

        private void Awake() => EnsureVolume();

        private void LateUpdate()
        {
            if (volume == null || _sensor == null) return;
            float target = _sensor.Current.CameraUnderwater ? 1f : 0f;
            float duration = profile != null ? profile.TransitionDuration : defaultTransitionDuration;
            volume.weight = Mathf.MoveTowards(volume.weight, target, Time.deltaTime / Mathf.Max(0.01f, duration));
        }

        private void EnsureVolume()
        {
            if (volume == null) volume = GetComponent<Volume>();
            if (volume == null || volume.sharedProfile != null) return;

            _runtimeVolumeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            _runtimeVolumeProfile.hideFlags = HideFlags.DontSave;
            ColorAdjustments color = _runtimeVolumeProfile.Add<ColorAdjustments>();
            color.active = true;
            color.colorFilter.overrideState = true;
            color.colorFilter.value = profile != null ? profile.Tint : new Color(0.15f, 0.55f, 0.65f, 1f);
            color.saturation.overrideState = true;
            float saturation = profile != null ? profile.Saturation : 0.65f;
            color.saturation.value = Mathf.Lerp(-60f, 0f, saturation);

            Vignette vignette = _runtimeVolumeProfile.Add<Vignette>();
            vignette.active = true;
            vignette.intensity.overrideState = true;
            vignette.intensity.value = profile != null ? profile.Vignette : 0.2f;
            vignette.smoothness.overrideState = true;
            vignette.smoothness.value = 0.7f;

            volume.isGlobal = true;
            volume.priority = 100f;
            volume.weight = 0f;
            volume.sharedProfile = _runtimeVolumeProfile;
        }

        private void OnDestroy()
        {
            if (_runtimeVolumeProfile == null) return;
            if (Application.isPlaying) Destroy(_runtimeVolumeProfile);
            else DestroyImmediate(_runtimeVolumeProfile);
        }
    }
}
