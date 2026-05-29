using DG.Tweening;
using UnityEngine;

namespace _001_Scripts.UI.Component
{
    public class PositionMove : UIComponentBase
    {
        [SerializeField] private int offset = 10;
        [SerializeField] private Vector2 moveVector = Vector2.up;
        [SerializeField] private Ease transition = Ease.OutQuad;

        public override void FadeIn()
        {
            if (_tweenOut != null)
                _tweenOut.Kill();

            _tweenOut = DOTween.Sequence()
                .Append(_rectTrs.DOAnchorPos(originPos + moveVector * offset, 2).SetEase(transition))
                .OnComplete(() =>
                {
                    canvasGroup.interactable = false;
                    canvasGroup.blocksRaycasts = false;
                    isViz = false;
                });
        }

        public override void FadeOut()
        {
            _rectTrs.anchoredPosition = originPos + moveVector * offset;

            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            if (_tweenIn != null)
                _tweenIn.Kill();

            _tweenIn = DOTween.Sequence()
                .Append(_rectTrs.DOAnchorPos(originPos, 1).SetEase(transition))
                .OnComplete(() =>
                {
                    canvasGroup.interactable = true;
                    canvasGroup.blocksRaycasts = true;
                    isViz = true;
                });
        }
    }
}