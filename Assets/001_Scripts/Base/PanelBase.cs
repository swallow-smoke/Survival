using System;
using DG.Tweening;
using UnityEngine;

namespace _001_Scripts.Base
{
    public abstract class PanelBase : MonoBehaviour
    {
        private Tween _tween;
        [SerializeField] protected CanvasGroup _canvasGroup;
        [SerializeField] private float duration;
        public Action onOpenComplete;
        public Action onCloseComplete;

        public virtual void Open()
        {
            if (_tween != null)
                _tween.Kill();

            _tween = DOTween.To(() => _canvasGroup.alpha, x => _canvasGroup.alpha = x, 1, duration)
                .SetEase(Ease.OutQuad).SetTarget(this).OnComplete(() =>
            {
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
                onOpenComplete?.Invoke();
            });
        }

        public virtual void Close()
        {
            if (_tween != null)
                _tween.Kill();

            _tween = DOTween.To(() => _canvasGroup.alpha, x => _canvasGroup.alpha = x, 0, duration)
                .SetEase(Ease.OutQuad).SetTarget(this).OnComplete(() =>
                {
                    _canvasGroup.interactable = false;
                    _canvasGroup.blocksRaycasts = false;
                    onCloseComplete?.Invoke();
                });
        }

        protected void OnDestroy()
        {
            _tween?.Kill();
        }
    }
}