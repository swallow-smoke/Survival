using System.Collections.Generic;
using System.Linq;
using AstraNope.Contracts;
using UnityEngine;

namespace AstraNope.UI.Base
{
    public abstract class PanelBase : MonoBehaviour
    {
        private List<IUIAnimator> _animator;
        private CanvasGroup _canvasGroup;
        private RectTransform _rectTransform;
        public bool isViz;

        protected void Awake()
        {
            _animator = GetComponentsInChildren<IUIAnimator>().ToList();
            _canvasGroup = GetComponent<CanvasGroup>();
            _rectTransform = transform as RectTransform;
            // Debug.Log(_animator.Count);
        }

        public virtual void Open()
        {
            EnsureRuntimeReferences();
            if (_rectTransform) _rectTransform.localScale = Vector3.one;

            // Make the panel visible immediately. Animators are enhancement only and
            // must not be able to leave a serialized CanvasGroup stuck at alpha zero.
            if (_canvasGroup)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
            }

            if (_animator.Count > 0)
                _animator.ForEach(panel => panel.FadeIn());
            isViz = true;
        }

        public virtual void Close()
        {
            EnsureRuntimeReferences();
            if (_animator.Count > 0)
                _animator.ForEach(panel => panel.FadeOut());
            else if (_canvasGroup)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }
            isViz = false;
        }

        private void EnsureRuntimeReferences()
        {
            _animator ??= GetComponentsInChildren<IUIAnimator>(true).ToList();
            if (!_canvasGroup) _canvasGroup = GetComponent<CanvasGroup>();
            if (!_rectTransform) _rectTransform = transform as RectTransform;
        }
    }
}
