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
        private Tween _tweenIn;
        private Tween _tweenOut;


        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image image;
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

            image.fillAmount = value / 100;
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
            _tweenOut?.Kill();
            _tweenIn?.Kill();
        }

        private void FadeOut()
        {
            if (_tweenOut != null)
                _tweenOut.Kill();

            _tweenOut = DOTween.Sequence()
                .Append(DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 0, 2)
                    .SetEase(Ease.OutQuad).OnComplete(() =>
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
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            
            if (_tweenIn != null)
                _tweenIn.Kill();

            _tweenIn = DOTween.Sequence()
                .Append(DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 1, 4)
                    .SetEase(Ease.OutQuad).OnComplete(() =>
                    {
                        canvasGroup.interactable = true;
                        canvasGroup.blocksRaycasts = true;
                    }))
                .Join(_rectTrs.DOAnchorPos(originPos, 4).SetEase(Ease.OutQuad))
                .Join(_rectTrs.DOSizeDelta(originSize, 4).SetEase(Ease.OutQuad));
        }
    }
}