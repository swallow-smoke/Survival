using System.Collections.Generic;
using _001_Scripts.Data;
using _001_Scripts.Entities;
using UnityEngine;

namespace _001_Scripts.Structure
{
    [DisallowMultipleComponent]
    public sealed class ScannableTarget : MonoBehaviour
    {
        private static readonly int ScanProgressId = Shader.PropertyToID("_ScanProgress");
        private static readonly int ScanActiveId = Shader.PropertyToID("_ScanActive");
        private static readonly int ScanColorId = Shader.PropertyToID("_ScanColor");

        [Header("Scan Data")]
        [SerializeField] private string displayName = "미확인 오브젝트";
        [SerializeField, Min(.1f), Tooltip("이 오브젝트를 완료까지 스캔하는 데 필요한 시간(초)")]
        private float scanTime = 3f;
        [SerializeField, Tooltip("완료 시 수집할 Resources/Data/Logs.json의 로그 ID. 비워두면 로그를 수집하지 않습니다.")]
        private string unlockLogId;

        [SerializeField, Tooltip("WorldItem이면 ItemDataBase의 이름과 설명으로 도감 로그를 자동 생성합니다.")]
        private bool includeWorldItemLog = true;

        [SerializeField, Tooltip("로그, 청사진 진행도, 즉시 해금을 한 대상에 여러 개 지정할 수 있습니다.")]
        private List<ScanReward> rewards = new();

        [Header("Scene-authored Scan Box")]
        [SerializeField, Tooltip("대상의 메시를 복제해 Scan Grid Overlay 재질을 지정한 자식 Renderer들")]
        private Renderer[] gridRenderers;
        [SerializeField] private Color scanColor = new(.16f, .95f, 1f, .72f);
        [SerializeField] private Color sweepColor = new(1f, 1f, 1f, .58f);
        [SerializeField] private Transform horizontalSweep;
        [SerializeField] private Transform verticalSweep;
        [SerializeField] private Vector3 scanBoundsCenter;
        [SerializeField] private Vector3 scanBoundsSize = Vector3.one;

        private MaterialPropertyBlock properties;

        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(displayName)) return displayName;
                var entity = GetComponentInParent<Entity>();
                return entity && !string.IsNullOrWhiteSpace(entity.DisplayName) ? entity.DisplayName : name;
            }
        }
        public string UnlockLogId => unlockLogId;
        public bool IncludeWorldItemLog => includeWorldItemLog;
        public IReadOnlyList<ScanReward> Rewards => rewards;
        public float ScanTime => Mathf.Max(.1f, scanTime);
        public bool IsScanned { get; private set; }
        public bool HasScanBoxVisuals => gridRenderers is { Length: > 0 } && gridRenderers[0] &&
                                         horizontalSweep && verticalSweep;

        private void Awake() => SetVisual(0f, false);
        private void OnDisable() => SetVisual(0f, false);

        public void Configure(string targetName, float duration, string logId, Renderer[] overlays)
        {
            displayName = targetName;
            scanTime = Mathf.Max(.1f, duration);
            unlockLogId = logId;
            gridRenderers = overlays;
            SetVisual(0f, false);
        }

        public void ConfigureWorldItem(float duration, Renderer[] overlays)
        {
            displayName = string.Empty;
            scanTime = Mathf.Max(.1f, duration);
            unlockLogId = string.Empty;
            includeWorldItemLog = true;
            gridRenderers = overlays;
            SetVisual(0f, false);
        }

        public void ConfigureScanBox(Renderer volume, Transform horizontal, Transform vertical,
            Vector3 localCenter, Vector3 localSize)
        {
            gridRenderers = volume ? new[] { volume } : System.Array.Empty<Renderer>();
            horizontalSweep = horizontal;
            verticalSweep = vertical;
            scanBoundsCenter = localCenter;
            scanBoundsSize = new Vector3(
                Mathf.Max(.05f, localSize.x),
                Mathf.Max(.05f, localSize.y),
                Mathf.Max(.05f, localSize.z));
            SetVisual(0f, false);
        }

        public bool AddReward(ScanReward reward)
        {
            if (reward == null) return false;
            rewards ??= new List<ScanReward>();
            foreach (ScanReward existing in rewards)
            {
                if (existing == null || existing.type != reward.type) continue;
                if (reward.type == ScanRewardType.Log &&
                    string.Equals(existing.logId, reward.logId, System.StringComparison.OrdinalIgnoreCase)) return false;
                if (reward.type != ScanRewardType.Log && existing.blueprintId == reward.blueprintId) return false;
            }
            rewards.Add(reward);
            return true;
        }

        public void SetVisual(float progress, bool active)
        {
            properties ??= new MaterialPropertyBlock();
            float normalized = Mathf.Clamp01(progress);
            foreach (Renderer itemRenderer in gridRenderers ?? System.Array.Empty<Renderer>())
            {
                if (!itemRenderer) continue;
                itemRenderer.enabled = active && !IsScanned;
                itemRenderer.GetPropertyBlock(properties);
                properties.SetFloat(ScanProgressId, normalized);
                properties.SetFloat(ScanActiveId, active && !IsScanned ? 1f : 0f);
                properties.SetColor(ScanColorId, scanColor);
                itemRenderer.SetPropertyBlock(properties);
            }

            UpdateSweep(horizontalSweep, normalized, active, true);
            UpdateSweep(verticalSweep, normalized, active, false);
        }

        private void UpdateSweep(Transform sweep, float progress, bool active, bool horizontal)
        {
            if (!sweep) return;
            var sweepRenderer = sweep.GetComponent<Renderer>();
            if (sweepRenderer)
            {
                sweepRenderer.enabled = active && !IsScanned;
                sweepRenderer.GetPropertyBlock(properties);
                properties.SetFloat(ScanProgressId, progress);
                properties.SetFloat(ScanActiveId, active && !IsScanned ? 1f : 0f);
                properties.SetColor(ScanColorId, sweepColor);
                sweepRenderer.SetPropertyBlock(properties);
            }

            Vector3 position = scanBoundsCenter;
            if (horizontal)
                position.y += Mathf.Lerp(-scanBoundsSize.y * .5f, scanBoundsSize.y * .5f, progress);
            else
                position.x += Mathf.Lerp(-scanBoundsSize.x * .5f, scanBoundsSize.x * .5f, progress);
            sweep.localPosition = position;
        }

        public void MarkScanned()
        {
            IsScanned = true;
            SetVisual(1f, false);
        }
    }
}
