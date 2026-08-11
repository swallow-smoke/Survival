using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace _001_Scripts.UI.Component
{
    public readonly struct SimpleRadialEntry
    {
        public readonly string Id;
        public readonly string Icon;
        public readonly string Label;
        public readonly bool Interactable;
        public readonly Action Selected;

        public SimpleRadialEntry(string id, string icon, string label, Action selected, bool interactable = true)
        {
            Id = id;
            Icon = icon;
            Label = label;
            Selected = selected;
            Interactable = interactable;
        }
    }

    [DisallowMultipleComponent]
    public sealed class SimpleRadialMenuView : MonoBehaviour
    {
        private const string RootName = "SimpleRadialRoot";
        private static readonly Color CenterColor = new(.1f, .13f, .15f, .84f);
        private static readonly Color NodeColor = new(.12f, .17f, .19f, .82f);
        private static readonly Color DisabledColor = new(.08f, .1f, .11f, .62f);
        private static readonly Color StrokeColor = new(.62f, .82f, .86f, .58f);

        [SerializeField, Min(1f)] private float radius = 210f;
        [SerializeField, Min(1f)] private float centerSize = 104f;
        [SerializeField, Min(1f)] private float nodeSize = 92f;
        [SerializeField, Min(0f)] private float strokeWidth = 1.5f;
        [SerializeField, Range(.05f, .5f)] private float openDuration = .18f;
        [SerializeField, Range(.05f, .5f)] private float closeDuration = .12f;
        [SerializeField, Range(0f, .1f)] private float nodeDelay = .025f;

        private RectTransform _root;
        private RectTransform _menu;
        private Transform _nodes;
        private Text _centerIcon;
        private CanvasGroup _rootGroup;
        private Button _outsideButton;
        private Action _outsideClicked;
        private Coroutine _animation;

        public void Rebuild(string centerIcon)
        {
            RemoveRoot();
            Build(centerIcon);
        }

        public void Ensure(string centerIcon)
        {
            if (!_root)
            {
                var existing = transform.Find(RootName);
                if (existing)
                {
                    _root = existing as RectTransform;
                    _menu = existing.Find("Menu") as RectTransform;
                    _nodes = existing.Find("Menu/Nodes");
                    _centerIcon = existing.Find("Menu/Center/Icon")?.GetComponent<Text>();
                    _rootGroup = existing.GetComponent<CanvasGroup>();
                    _outsideButton = existing.Find("Outside")?.GetComponent<Button>();
                }
            }

            if (!_root || !_menu || !_nodes || !_centerIcon || !_rootGroup || !_outsideButton) Rebuild(centerIcon);
            else _centerIcon.text = centerIcon;
            BindOutsideClick();
        }

        public void SetOutsideClick(Action callback)
        {
            _outsideClicked = callback;
            BindOutsideClick();
        }

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

        public void SetEntries(IReadOnlyList<SimpleRadialEntry> entries)
        {
            if (!_nodes) return;
            ClearChildren(_nodes);
            if (entries == null || entries.Count == 0) return;

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                float angle = 90f - i * 360f / entries.Count;
                float radians = angle * Mathf.Deg2Rad;
                Vector2 position = new(Mathf.Cos(radians) * radius, Mathf.Sin(radians) * radius);
                CreateNode(entry, position);
            }
        }

        private void Build(string centerIcon)
        {
            _root = Rect(RootName, transform);
            Stretch(_root);
            _rootGroup = _root.gameObject.AddComponent<CanvasGroup>();

            var outside = new GameObject("Outside", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(Button));
            outside.layer = 5;
            outside.transform.SetParent(_root, false);
            var outsideRect = outside.GetComponent<RectTransform>();
            Stretch(outsideRect);
            var outsideImage = outside.GetComponent<Image>();
            outsideImage.color = new Color(0f, 0f, 0f, .001f);
            _outsideButton = outside.GetComponent<Button>();
            _outsideButton.targetGraphic = outsideImage;
            _outsideButton.transition = Selectable.Transition.None;

            _menu = Rect("Menu", _root);
            Anchor(_menu, new Vector2(.5f, .5f), new Vector2(640f, 640f), Vector2.zero);
            _nodes = Rect("Nodes", _menu);
            Stretch((RectTransform)_nodes);

            var center = Circle("Center", _menu, centerSize, CenterColor);
            AddStroke(center);
            center.raycastTarget = true;
            _centerIcon = Label("Icon", center.transform, centerIcon, 34, TextAnchor.MiddleCenter);
            Stretch(_centerIcon.rectTransform);
            BindOutsideClick();
            SetFinalVisualState();
        }

        private void CreateNode(SimpleRadialEntry entry, Vector2 position)
        {
            var circle = Circle(string.IsNullOrWhiteSpace(entry.Id) ? "Node" : entry.Id, _nodes, nodeSize,
                entry.Interactable ? NodeColor : DisabledColor);
            circle.rectTransform.anchoredPosition = position;
            var button = circle.gameObject.AddComponent<Button>();
            button.targetGraphic = circle;
            button.interactable = entry.Interactable;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.24f, 1.24f, 1.24f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color(.82f, .88f, .9f, 1f);
            colors.disabledColor = new Color(.65f, .65f, .65f, .7f);
            colors.fadeDuration = .08f;
            button.colors = colors;
            if (entry.Selected != null) button.onClick.AddListener(() => entry.Selected());
            circle.gameObject.AddComponent<CanvasGroup>();
            AddStroke(circle);

            string text = string.IsNullOrWhiteSpace(entry.Label)
                ? entry.Icon
                : $"{entry.Icon}\n{entry.Label}";
            var label = Label("Content", circle.transform, text, 13, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform);
        }

        private void AddStroke(Graphic graphic)
        {
            var outline = graphic.gameObject.AddComponent<Outline>();
            outline.effectColor = StrokeColor;
            outline.effectDistance = Vector2.one * strokeWidth;
            outline.useGraphicAlpha = true;
        }

        private void BindOutsideClick()
        {
            if (!_outsideButton) return;
            _outsideButton.onClick.RemoveAllListeners();
            _outsideButton.onClick.AddListener(() => _outsideClicked?.Invoke());
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

        private static RadialCircleGraphic Circle(string name, Transform parent, float size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RadialCircleGraphic));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            var circle = go.GetComponent<RadialCircleGraphic>();
            circle.color = color;
            circle.InnerRadius = 0f;
            Anchor(circle.rectTransform, new Vector2(.5f, .5f), Vector2.one * size, Vector2.zero);
            return circle;
        }

        private static Text Label(string name, Transform parent, string value, int size, TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.color = Color.white;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private void RemoveRoot()
        {
            if (_animation != null)
            {
                StopCoroutine(_animation);
                _animation = null;
            }
            var existing = transform.Find(RootName);
            if (!existing) return;
            existing.gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(existing.gameObject);
            else DestroyImmediate(existing.gameObject);
            _root = null;
            _menu = null;
            _nodes = null;
            _centerIcon = null;
            _rootGroup = null;
            _outsideButton = null;
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i).gameObject;
                child.SetActive(false);
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Anchor(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 position)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            rect.localScale = Vector3.one;
        }
    }
}
