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
        private bool isViz = false;


        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image image;
        [SerializeField] private RectTransform _rectTrs;
        [SerializeField] private int upOffSet = 10;
        private Vector2 originPos;

        private void Awake()
        {
            originPos = _rectTrs.anchoredPosition;
        }

        public async UniTaskVoid StatUpdate(float value)
        {
            _cts?.Cancel();
            _cts?.Dispose();

            image.fillAmount = value / 100;

            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            if (!isViz && (_tweenIn == null || !_tweenIn.IsPlaying()) && (_tweenOut == null || !_tweenOut.IsPlaying())) FadeIn();
            
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
                .Append(DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 0, 1)
                    .SetEase(Ease.OutQuad).OnComplete(() =>
                    {
                        canvasGroup.interactable = false;
                        canvasGroup.blocksRaycasts = false;
                        isViz = false;
                    }))
                .Join(_rectTrs.DOAnchorPos(originPos + Vector2.up * upOffSet, 2).SetEase(Ease.OutCirc));
        }

        private void FadeIn()
        {
            _rectTrs.anchoredPosition = originPos + Vector2.up * upOffSet;
            
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            if (_tweenIn != null)
                _tweenIn.Kill();

            _tweenIn = DOTween.Sequence()
                .Append(DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 1, 1)
                    .SetEase(Ease.OutQuad).OnComplete(() =>
                    {
                        canvasGroup.interactable = true;
                        canvasGroup.blocksRaycasts = true;
                        isViz = true;
                    }))
                .Join(_rectTrs.DOAnchorPos(originPos, 1).SetEase(Ease.OutCirc));
        }
    }
}