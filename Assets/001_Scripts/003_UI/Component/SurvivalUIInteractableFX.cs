using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _001_Scripts.UI.Component
{
    public sealed class SurvivalUIInteractableFX : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [SerializeField, Min(1f)] private float hoverScale = 1.025f;
        [SerializeField, Range(.8f, 1f)] private float pressedScale = .97f;
        [SerializeField, Min(1f)] private float responseSpeed = 18f;
        [SerializeField, Range(0, 10)] private int clickParticleCount = 6;
        [SerializeField] private Color clickParticleColor = new(.58f, .82f, 1f, .95f);

        private RectTransform _rect;
        private Selectable _selectable;
        private Vector3 _baseScale;
        private Vector3 _targetScale;
        private bool _hovered;

        private void Awake()
        {
            _rect = transform as RectTransform;
            _selectable = GetComponent<Selectable>();
            _baseScale = _rect ? _rect.localScale : transform.localScale;
            _targetScale = _baseScale;
        }

        private void OnEnable()
        {
            if (!_rect) _rect = transform as RectTransform;
            _baseScale = _rect ? _rect.localScale : transform.localScale;
            _targetScale = _baseScale;
        }

        private void Update()
        {
            if (!_rect) return;
            _rect.localScale = Vector3.Lerp(_rect.localScale, _targetScale,
                1f - Mathf.Exp(-responseSpeed * Time.unscaledDeltaTime));
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!CanInteract()) return;
            _hovered = true;
            _targetScale = _baseScale * hoverScale;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            _targetScale = _baseScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!CanInteract() || eventData.button != PointerEventData.InputButton.Left) return;
            _targetScale = _baseScale * pressedScale;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _targetScale = _hovered && CanInteract() ? _baseScale * hoverScale : _baseScale;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!CanInteract() || eventData.button != PointerEventData.InputButton.Left) return;
            SpawnClickParticles(eventData);
        }

        private void SpawnClickParticles(PointerEventData eventData)
        {
            var canvas = GetComponentInParent<Canvas>();
            var canvasRect = canvas ? canvas.transform as RectTransform : null;
            if (!canvasRect || clickParticleCount <= 0) return;

            Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, eventData.position, eventCamera, out var origin)) return;

            var sourceImage = _selectable?.targetGraphic as Image;
            for (int i = 0; i < clickParticleCount; i++)
            {
                var particleObject = new GameObject("UI_ClickSpark", typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(Image));
                particleObject.layer = gameObject.layer;
                particleObject.transform.SetParent(canvasRect, false);
                particleObject.transform.SetAsLastSibling();

                var particleRect = particleObject.GetComponent<RectTransform>();
                float size = Random.Range(4f, 9f);
                particleRect.sizeDelta = new Vector2(size, size);
                particleRect.anchoredPosition = origin;
                particleRect.localScale = Vector3.one;

                var particleImage = particleObject.GetComponent<Image>();
                particleImage.sprite = sourceImage ? sourceImage.sprite : null;
                particleImage.type = sourceImage && sourceImage.sprite ? Image.Type.Sliced : Image.Type.Simple;
                particleImage.color = Color.Lerp(clickParticleColor,
                    new Color(.78f, .52f, 1f, .95f), Random.value);
                particleImage.raycastTarget = false;

                float angle = (360f / clickParticleCount) * i + Random.Range(-18f, 18f);
                float distance = Random.Range(24f, 48f);
                Vector2 destination = origin + new Vector2(
                    Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * distance;
                float duration = Random.Range(.28f, .44f);
                particleRect.DOAnchorPos(destination, duration).SetEase(Ease.OutCubic).SetUpdate(true);
                particleRect.DOScale(.15f, duration).SetEase(Ease.InQuad).SetUpdate(true);
                particleImage.DOFade(0f, duration).SetEase(Ease.InQuad).SetUpdate(true)
                    .OnComplete(() => Destroy(particleObject));
            }
        }

        private bool CanInteract() => !_selectable || _selectable.IsInteractable();

        private void OnDisable()
        {
            if (_rect) _rect.localScale = _baseScale;
            _hovered = false;
        }
    }
}
