using System;
using System.Threading;
using DG.Tweening;
using UnityEngine;

namespace _001_Scripts.UI.Component
{
    public abstract class UIComponentBase : MonoBehaviour
    {
        protected Tween _tweenIn;
        protected Tween _tweenOut;
        protected bool isViz = false;
        protected Vector2 originPos;
        protected CanvasGroup canvasGroup;
        
        
        protected RectTransform _rectTrs;

        protected virtual void Awake()
        {
            canvasGroup = GetComponentInParent<CanvasGroup>();
            _rectTrs = GetComponent<RectTransform>();
            originPos = _rectTrs.anchoredPosition;
        }
        
        public abstract void FadeIn();
        public abstract void FadeOut();

        protected void OnDestroy()
        {
            _tweenIn?.Kill();
            _tweenOut?.Kill();
        }
    }
}