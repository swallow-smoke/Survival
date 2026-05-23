using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace _001_Scripts.UI.Component
{
    public class Stamina : MonoBehaviour
    {
        [SerializeField, Tooltip("1000 = 1sec")]
        private int hideTime = 5000;

        private CancellationTokenSource _cts;
        private Tween _tween;


        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Slider _slider;
        [SerializeField] private RectTransform _rectTrs;
        [SerializeField] private int upOffSet = 10;
        private Vector2 originPos;
        private Vector2 originSize;

        private void Awake()
        {
            originPos = _rectTrs.anchoredPosition;
            originSize = _rectTrs.sizeDelta;
        }

        public async UniTaskVoid StatUpdate(float value)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            FadeIn();

            _slider.value = value;
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            var cancelled = await UniTask.Delay(millisecondsDelay: hideTime, cancellationToken: token)
                .SuppressCancellationThrow();

            if (cancelled)
                return;

            FadeOut();
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _tween?.Kill();
        }

        private void FadeOut()
        {
            if (_tween != null)
                _tween.Kill();

            _tween = DOTween.Sequence()
                .Append(DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 0, 4)
                    .SetEase(Ease.OutQuad).SetTarget(this).OnComplete(() =>
                    {
                        canvasGroup.interactable = false;
                        canvasGroup.blocksRaycasts = false;
                    }));
        }

        private void FadeIn()
        {
            if (canvasGroup.alpha >= 0.99f) return;

            _rectTrs.anchoredPosition = originPos + Vector2.up * upOffSet;
            _rectTrs.sizeDelta = new Vector2(0, originSize.y);
            canvasGroup.alpha = 0;
            
            if (_tween != null)
                _tween.Kill();

            _tween = DOTween.Sequence()
                .Append(DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 1, 4)
                    .SetEase(Ease.OutQuad).SetTarget(this).OnComplete(() =>
                    {
                        canvasGroup.interactable = true;
                        canvasGroup.blocksRaycasts = true;
                    }))
                .Join(_rectTrs.DOAnchorPos(originPos, 5).SetEase(Ease.OutQuad).SetTarget(this))
                .Join(_rectTrs.DOSizeDelta(originSize, 5).SetEase(Ease.OutQuad).SetTarget(this));
        }
    }
}