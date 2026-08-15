using System;
using UnityEngine;

namespace _001_Scripts.Controller
{
    public enum FirstPersonItemAction
    {
        Use,
        Harvest
    }

    [DisallowMultipleComponent]
    public sealed class FirstPersonItemMotion : MonoBehaviour
    {
        [SerializeField, Tooltip("The scene-authored transform moved by the procedural animation.")]
        private Transform animatedRoot;

        [Header("Equip")]
        [SerializeField] private Vector3 equipPositionOffset = new(0.04f, -0.24f, -0.12f);
        [SerializeField] private Vector3 equipRotationOffset = new(18f, -12f, 10f);
        [SerializeField, Min(0.01f)] private float equipDuration = 0.24f;

        [Header("Use")]
        [SerializeField] private Vector3 usePositionOffset = new(0.01f, 0.015f, 0.13f);
        [SerializeField] private Vector3 useRotationOffset = new(-16f, 2f, 4f);
        [SerializeField, Min(0.01f)] private float useDuration = 0.28f;

        [Header("Harvest")]
        [SerializeField] private Vector3 harvestPositionOffset = new(-0.07f, -0.04f, 0.16f);
        [SerializeField] private Vector3 harvestRotationOffset = new(-28f, 18f, 24f);
        [SerializeField, Min(0.01f)] private float harvestDuration = 0.36f;

        [Header("Unequip")]
        [SerializeField, Min(0.01f)] private float unequipDuration = 0.18f;

        private Vector3 _restPosition;
        private Quaternion _restRotation;
        private Vector3 _fromPosition;
        private Quaternion _fromRotation;
        private Vector3 _peakPosition;
        private Quaternion _peakRotation;
        private float _duration;
        private float _elapsed;
        private MotionKind _kind;
        private Action _onComplete;
        private bool _hasRestPose;

        public bool IsAnimating => _kind != MotionKind.None;
        private Transform Root => animatedRoot ? animatedRoot : transform;

        private enum MotionKind { None, Equip, Action, Unequip }

        private void Awake() => CaptureRestPose();

        public void Configure(Transform root)
        {
            animatedRoot = root;
            CaptureRestPose(force: true);
        }

        public void CaptureRestPose(bool force = false)
        {
            if (_hasRestPose && !force) return;
            _restPosition = Root.localPosition;
            _restRotation = Root.localRotation;
            _hasRestPose = true;
        }

        public void PlayEquip()
        {
            CaptureRestPose();
            Cancel(resetPose: false);
            Root.localPosition = _restPosition + equipPositionOffset;
            Root.localRotation = _restRotation * Quaternion.Euler(equipRotationOffset);
            Begin(MotionKind.Equip, _restPosition, _restRotation, equipDuration, null);
        }

        public void Play(FirstPersonItemAction action)
        {
            CaptureRestPose();
            Vector3 positionOffset = action == FirstPersonItemAction.Harvest
                ? harvestPositionOffset : usePositionOffset;
            Vector3 rotationOffset = action == FirstPersonItemAction.Harvest
                ? harvestRotationOffset : useRotationOffset;
            float duration = action == FirstPersonItemAction.Harvest ? harvestDuration : useDuration;

            Cancel(resetPose: true);
            _fromPosition = _restPosition;
            _fromRotation = _restRotation;
            _peakPosition = _restPosition + positionOffset;
            _peakRotation = _restRotation * Quaternion.Euler(rotationOffset);
            _duration = duration;
            _elapsed = 0f;
            _kind = MotionKind.Action;
        }

        public void PlayUnequip(Action onComplete)
        {
            CaptureRestPose();
            Cancel(resetPose: true);
            Begin(MotionKind.Unequip, _restPosition + equipPositionOffset,
                _restRotation * Quaternion.Euler(equipRotationOffset), unequipDuration, onComplete);
        }

        public void Cancel(bool resetPose)
        {
            _onComplete = null;
            _kind = MotionKind.None;
            if (resetPose && _hasRestPose)
            {
                Root.localPosition = _restPosition;
                Root.localRotation = _restRotation;
            }
        }

        private void Begin(MotionKind kind, Vector3 targetPosition, Quaternion targetRotation,
            float duration, Action onComplete)
        {
            _fromPosition = Root.localPosition;
            _fromRotation = Root.localRotation;
            _peakPosition = targetPosition;
            _peakRotation = targetRotation;
            _duration = duration;
            _elapsed = 0f;
            _kind = kind;
            _onComplete = onComplete;

            if (!Application.isPlaying)
                Complete();
        }

        private void LateUpdate()
        {
            if (_kind == MotionKind.None) return;
            _elapsed += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(_elapsed / Mathf.Max(0.01f, _duration));

            if (_kind == MotionKind.Action)
            {
                float wave = Mathf.Sin(normalized * Mathf.PI);
                float easedWave = 1f - (1f - wave) * (1f - wave);
                Root.localPosition = Vector3.LerpUnclamped(_restPosition, _peakPosition, easedWave);
                Root.localRotation = Quaternion.SlerpUnclamped(_restRotation, _peakRotation, easedWave);
            }
            else
            {
                float eased = 1f - Mathf.Pow(1f - normalized, 3f);
                Root.localPosition = Vector3.LerpUnclamped(_fromPosition, _peakPosition, eased);
                Root.localRotation = Quaternion.SlerpUnclamped(_fromRotation, _peakRotation, eased);
            }

            if (normalized >= 1f) Complete();
        }

        private void Complete()
        {
            MotionKind completed = _kind;
            _kind = MotionKind.None;
            if (completed != MotionKind.Unequip)
            {
                Root.localPosition = _restPosition;
                Root.localRotation = _restRotation;
            }
            Action callback = _onComplete;
            _onComplete = null;
            callback?.Invoke();
        }

        private void OnDisable() => Cancel(resetPose: true);
    }
}
