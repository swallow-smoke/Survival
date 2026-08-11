using System;
using _001_Scripts.Base;
using _001_Scripts.Data.Message;
using _001_Scripts.Data.Message.Player;
using _001_Scripts.Type.States;
using _001_Scripts.UI.Component;
using MessagePipe;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _001_Scripts.UI
{
    public class InteractionPanel : PanelBase
    {
        [SerializeField] private TMP_Text labelText;

        private IDisposable _bag;
        private CrosshairGraphic _crosshair;
        private RectTransform _reticleTransform;
        private Text _prompt;
        private CanvasGroup _canvasGroup;
        private bool _hasInteraction;

        private new void Awake()
        {
            base.Awake();
            if (labelText) labelText.gameObject.SetActive(false);
            transform.localScale = Vector3.one;
            _canvasGroup = GetComponent<CanvasGroup>();
            if (!_canvasGroup) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            isViz = true;
            ConfigureCanvas();
            BuildCrosshair();
        }

        private void ConfigureCanvas()
        {
            var canvas = GetComponent<Canvas>();
            if (!canvas) canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 210;
            SurvivalUITheme.ConfigureCanvas(gameObject, 1600f, 900f);
        }

        private void BuildCrosshair()
        {
            var reticle = new GameObject("Crosshair", typeof(RectTransform), typeof(CanvasRenderer), typeof(CrosshairGraphic));
            _reticleTransform = reticle.GetComponent<RectTransform>();
            _reticleTransform.SetParent(transform, false);
            _reticleTransform.anchorMin = _reticleTransform.anchorMax = new Vector2(.5f, .5f);
            _reticleTransform.pivot = new Vector2(.5f, .5f);
            _reticleTransform.anchoredPosition = Vector2.zero;
            _reticleTransform.sizeDelta = new Vector2(64f, 64f);
            _crosshair = reticle.GetComponent<CrosshairGraphic>();
            _crosshair.color = new Color(.94f, .97f, 1f, .9f);
            _crosshair.raycastTarget = false;

            var promptObject = new GameObject("InteractionPrompt", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            var promptRect = promptObject.GetComponent<RectTransform>();
            promptRect.SetParent(transform, false);
            promptRect.anchorMin = promptRect.anchorMax = new Vector2(.5f, .5f);
            promptRect.pivot = new Vector2(.5f, 1f);
            promptRect.anchoredPosition = new Vector2(0f, -40f);
            promptRect.sizeDelta = new Vector2(520f, 42f);
            _prompt = promptObject.GetComponent<Text>();
            _prompt.font = SurvivalUITheme.Font;
            _prompt.fontSize = 16;
            _prompt.fontStyle = FontStyle.Bold;
            _prompt.alignment = TextAnchor.UpperCenter;
            _prompt.color = new Color(.78f, .92f, 1f, 1f);
            _prompt.raycastTarget = false;
            _prompt.gameObject.SetActive(false);
        }

        [Inject]
        public void Constructor(ISubscriber<InteractionUIMessage> interactionSubscriber,
            ISubscriber<PlayerUIStateMsg> uiStateSubscriber)
        {
            _bag?.Dispose();
            var builder = DisposableBag.CreateBuilder();
            builder.Add(interactionSubscriber.Subscribe(OnMessage));
            builder.Add(uiStateSubscriber.Subscribe(OnUIState));
            _bag = builder.Build();
        }

        private void OnMessage(InteractionUIMessage message)
        {
            _hasInteraction = message.isVisible;
            _crosshair.Interaction = _hasInteraction;
            _crosshair.color = _hasInteraction
                ? new Color(.3f, .88f, 1f, 1f)
                : new Color(.94f, .97f, 1f, .9f);
            _prompt.gameObject.SetActive(_hasInteraction);
            _prompt.text = !_hasInteraction
                ? string.Empty
                : string.IsNullOrWhiteSpace(message.promptKey)
                    ? message.label
                    : $"[{message.promptKey}]  {message.label}";
        }

        private void OnUIState(PlayerUIStateMsg message)
        {
            bool gameplay = message.state == PlayerUIState.None;
            _canvasGroup.alpha = gameplay ? 1f : 0f;
        }

        private void Update()
        {
            if (!_reticleTransform) return;
            float scale = _hasInteraction ? 1f + Mathf.Sin(Time.unscaledTime * 5f) * .045f : 1f;
            _reticleTransform.localScale = Vector3.one * scale;
        }

        private void OnDestroy() => _bag?.Dispose();
    }
}
