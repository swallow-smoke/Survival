using System;
using System.Threading;
using _001_Scripts.Interface;
using DG.Tweening;
using UnityEngine;

namespace _001_Scripts.UI.Component
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIComponentBase : MonoBehaviour, IUIAnimator
    {
        protected Tween _tweenIn;
        protected Tween _tweenOut;
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