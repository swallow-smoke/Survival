using System;
using _001_Scripts.Base;
using _001_Scripts.Data.Message;
using MessagePipe;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _001_Scripts.UI
{
    public class HUDPanel : PanelBase
    {
        [Header("Oxygen")]
        [SerializeField] private RectTransform oxygenFill;
        [SerializeField] private Text oxygenValue;

        [Header("Health")]
        [SerializeField] private RectTransform healthFill;
        [SerializeField] private Text healthValue;

        [Header("Hunger")]
        [SerializeField] private RectTransform hungerFill;
        [SerializeField] private Text hungerValue;

        [Header("Hydration")]
        [SerializeField] private RectTransform hydrationFill;
        [SerializeField] private Text hydrationValue;

        [Header("Display")]
        [SerializeField, Range(0f, 1f)] private float warningThreshold = .25f;
        [SerializeField] private Color warningColor = new(1f, .42f, .72f, 1f);
        [SerializeField, HideInInspector] private int editorStyleVersion;

        private IDisposable _bag;

        private void Awake()
        {
            base.Awake();
            NormalizeRootScale();
            ApplyFont();
        }

        private void OnValidate()
        {
            NormalizeRootScale();
            ApplyFont();
        }

        private void NormalizeRootScale()
        {
            if (transform is RectTransform rect && rect.localScale != Vector3.one)
                rect.localScale = Vector3.one;
        }

        private void ApplyFont()
        {
            foreach (var label in GetComponentsInChildren<Text>(true))
                label.font = SurvivalUITheme.Font;
        }

        [Inject]
        public void Constructor(ISubscriber<PlayerStatMessage> playerStatSubscriber)
        {
            _bag?.Dispose();
            var builder = DisposableBag.CreateBuilder();
            builder.Add(playerStatSubscriber.Subscribe(UIUpdate));
            _bag = builder.Build();
        }

        private void UIUpdate(PlayerStatMessage msg)
        {
            UpdateStat(healthFill, healthValue, msg.hp);
            UpdateStat(hungerFill, hungerValue, msg.hungry);
            UpdateStat(hydrationFill, hydrationValue, msg.water);
            UpdateStat(oxygenFill, oxygenValue, msg.oxygen);
        }

        private void UpdateStat(RectTransform fill, Text valueLabel, float value)
        {
            float normalized = Mathf.Clamp01(value / 100f);
            if (fill)
            {
                fill.anchorMax = new Vector2(normalized, 1f);
                fill.offsetMax = Vector2.zero;
            }

            if (!valueLabel) return;
            valueLabel.text = $"{Mathf.RoundToInt(value)}%";
            var fillImage = fill ? fill.GetComponent<Image>() : null;
            var normalColor = fillImage ? fillImage.color : Color.white;
            valueLabel.color = normalized <= warningThreshold ? warningColor : normalColor;
        }

        private void OnDestroy() => _bag?.Dispose();
    }

    public static class SurvivalUITheme
    {
        public static readonly Color Cyan = new(.66f, .46f, 1f, 1f);
        public static readonly Color Border = new(.82f, .72f, 1f, .42f);
        public static readonly Color Danger = new(1f, .42f, .72f, 1f);
        public static readonly Color TextMuted = new(.70f, .63f, .82f, 1f);

        private static Font _font;

        public static Font Font
        {
            get
            {
                if (_font) return _font;
                _font = Font.CreateDynamicFontFromOSFont(
                    new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Noto Sans CJK KR", "Arial" }, 32);
                return _font;
            }
        }

        public static void ConfigureCanvas(GameObject target, float width, float height)
        {
            var scaler = target.GetComponent<CanvasScaler>();
            if (!scaler) return;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(width, height);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = .5f;
        }
    }
}
