using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace _001_Scripts.Base
{
    public abstract class PanelBase : MonoBehaviour
    {
        private Tween _tween;
        public string panelName;
        [SerializeField] protected CanvasGroup _canvasGroup;
        [SerializeField] private float duration;
        protected bool isVisible;

        public virtual void Open()
        {
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
            
            if (_tween != null)
                _tween.Kill();

            _tween = DOTween.To(() => _canvasGroup.alpha, x => _canvasGroup.alpha = x, 1, duration)
                .SetEase(Ease.OutQuad).SetTarget(this);
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
                });
        }

        protected void OnDestroy()
        {
            _tween?.Kill();
        }
    }
}