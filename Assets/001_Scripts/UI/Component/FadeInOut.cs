using DG.Tweening;
using UnityEngine;

namespace _001_Scripts.UI.Component
{
    public class FadeInOut : UIComponentBase
    {
        [SerializeField] private float hideTime = 1000f;
        [SerializeField] private Ease transition = Ease.OutQuad;
        
        public override void FadeIn()
        {
            if (_tweenOut != null)
                _tweenOut.Kill();

            _tweenOut = DOTween.Sequence()
                .Append(DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 1, 1)
                    .OnComplete(() =>
                    {
                        canvasGroup.interactable = false;
                        canvasGroup.blocksRaycasts = false;
                        isViz = false;
                    }));
        }
        
        public override void FadeOut()
        {
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            if (_tweenIn != null)
                _tweenIn.Kill();

            _tweenIn = DOTween.Sequence()
                .Append(DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 0, 1)
                    .SetEase(transition).OnComplete(() =>
                    {
                        canvasGroup.interactable = true;
                        canvasGroup.blocksRaycasts = true;
                        isViz = true;
                    }));
        }
    }
}