using System;
using System.Collections;
using UnityEngine;

namespace AstraNope.UI.Components
{
    public sealed partial class SimpleRadialMenuView
    {
        public void PlayOpenAnimation()
        {
            if (!Application.isPlaying)
            {
                SetFinalVisualState();
                return;
            }
            if (_animation != null) StopCoroutine(_animation);
            _animation = StartCoroutine(AnimateOpen());
        }

        public void PlayNodeAnimation()
        {
            if (!Application.isPlaying) return;
            if (_animation != null) StopCoroutine(_animation);
            _animation = StartCoroutine(AnimateNodesOnly());
        }

        public void PlayCloseAnimation(Action finished)
        {
            if (!Application.isPlaying)
            {
                finished?.Invoke();
                return;
            }
            if (_animation != null) StopCoroutine(_animation);
            _animation = StartCoroutine(AnimateClose(finished));
        }
        private IEnumerator AnimateOpen()
        {
            _rootGroup.alpha = 0f;
            _menu.localScale = Vector3.one * .86f;
            PrepareNodes();
            float elapsed = 0f;
            while (elapsed < openDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Smooth(elapsed / openDuration);
                _rootGroup.alpha = t;
                _menu.localScale = Vector3.one * Mathf.Lerp(.86f, 1f, t);
                AnimateNodes(elapsed, openDuration);
                yield return null;
            }
            SetFinalVisualState();
            _animation = null;
        }

        private IEnumerator AnimateNodesOnly()
        {
            PrepareNodes();
            float duration = Mathf.Max(.1f, openDuration * .75f);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                AnimateNodes(elapsed, duration);
                yield return null;
            }
            FinishNodes();
            _animation = null;
        }

        private IEnumerator AnimateClose(Action finished)
        {
            float startAlpha = _rootGroup.alpha;
            Vector3 startScale = _menu.localScale;
            float elapsed = 0f;
            while (elapsed < closeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Smooth(elapsed / closeDuration);
                _rootGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
                _menu.localScale = Vector3.Lerp(startScale, Vector3.one * .9f, t);
                yield return null;
            }
            _rootGroup.alpha = 0f;
            _animation = null;
            finished?.Invoke();
        }

        private void PrepareNodes()
        {
            for (int i = 0; i < _nodes.childCount; i++)
            {
                var child = _nodes.GetChild(i);
                child.localScale = Vector3.one * .68f;
                child.GetComponent<CanvasGroup>().alpha = 0f;
            }
        }

        private void AnimateNodes(float elapsed, float duration)
        {
            for (int i = 0; i < _nodes.childCount; i++)
            {
                float t = Smooth((elapsed - i * nodeDelay) / Mathf.Max(.05f, duration * .72f));
                var child = _nodes.GetChild(i);
                child.localScale = Vector3.one * Mathf.Lerp(.68f, 1f, t);
                child.GetComponent<CanvasGroup>().alpha = t;
            }
        }

        private void FinishNodes()
        {
            for (int i = 0; i < _nodes.childCount; i++)
            {
                var child = _nodes.GetChild(i);
                child.localScale = Vector3.one;
                child.GetComponent<CanvasGroup>().alpha = 1f;
            }
        }

        private void SetFinalVisualState()
        {
            if (_rootGroup) _rootGroup.alpha = 1f;
            if (_menu) _menu.localScale = Vector3.one;
            if (_nodes) FinishNodes();
        }

        private static float Smooth(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }
    }
}