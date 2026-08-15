using System.Collections;
using _001_Scripts.Data.Message;
using UnityEngine;
using UnityEngine.UI;

namespace _001_Scripts.UI.Component
{
    [DisallowMultipleComponent]
    public sealed class LeftNotificationFeed : MonoBehaviour
    {
        private const float Width = 340f;
        private const float Height = 68f;
        private const int MaxVisible = 6;

        private RectTransform _root;

        public void EnsureView()
        {
            if (_root) return;
            var rootObject = new GameObject("LeftNotificationFeed", typeof(RectTransform), typeof(VerticalLayoutGroup));
            _root = rootObject.GetComponent<RectTransform>();
            _root.SetParent(transform, false);
            _root.anchorMin = _root.anchorMax = new Vector2(0f, .5f);
            _root.pivot = new Vector2(0f, .5f);
            _root.anchoredPosition = Vector2.zero;
            _root.sizeDelta = new Vector2(Width, 440f);
            var layout = rootObject.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            _root.SetAsLastSibling();
        }

        public void Enqueue(NotificationMessage message)
        {
            EnsureView();
            TrimForIncomingNotification();

            var wrapper = new GameObject("Notification", typeof(RectTransform));
            var wrapperRect = wrapper.GetComponent<RectTransform>();
            wrapperRect.SetParent(_root, false);
            wrapperRect.sizeDelta = new Vector2(Width, Height);

            var toast = new GameObject("Card", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(RightRoundedRectGraphic), typeof(Outline), typeof(CanvasGroup));
            var toastRect = toast.GetComponent<RectTransform>();
            toastRect.SetParent(wrapperRect, false);
            toastRect.anchorMin = new Vector2(0f, 0f);
            toastRect.anchorMax = new Vector2(0f, 1f);
            toastRect.pivot = new Vector2(0f, .5f);
            toastRect.sizeDelta = new Vector2(Width, 0f);
            toastRect.anchoredPosition = new Vector2(-Width, 0f);

            var background = toast.GetComponent<RightRoundedRectGraphic>();
            background.color = Background(message.Kind);
            background.raycastTarget = false;
            var outline = toast.GetComponent<Outline>();
            outline.effectColor = new Color(.7f, .82f, 1f, .28f);
            outline.effectDistance = new Vector2(1f, -1f);

            CreateText("Icon", toastRect, message.Icon, new Vector2(16f, 0f), new Vector2(48f, Height),
                25, TextAnchor.MiddleCenter, Color.white);
            CreateText("Title", toastRect, message.Title, new Vector2(70f, -9f), new Vector2(245f, 28f),
                16, TextAnchor.MiddleLeft, Color.white);
            CreateText("Body", toastRect, message.Body, new Vector2(70f, -35f), new Vector2(245f, 24f),
                13, TextAnchor.MiddleLeft, new Color(.78f, .84f, .94f, 1f));

            Image lifetimeProgress = CreateLifetimeProgress(toastRect, message.Kind);
            StartCoroutine(Animate(wrapper, toastRect, toast.GetComponent<CanvasGroup>(), lifetimeProgress,
                message.Duration));
        }

        private void TrimForIncomingNotification()
        {
            // Destroy is deferred until the end of the frame in Play Mode. Keeping it inside a
            // childCount-based while loop therefore never changes childCount and allocates until OOM.
            int removeCount = Mathf.Max(0, _root.childCount - MaxVisible + 1);
            for (int i = 0; i < removeCount; i++)
            {
                Transform oldest = _root.GetChild(0);
                oldest.gameObject.SetActive(false);
                oldest.SetParent(null, false); // Update childCount immediately before deferred destruction.
                if (Application.isPlaying) Destroy(oldest.gameObject);
                else DestroyImmediate(oldest.gameObject);
            }
        }

        private IEnumerator Animate(GameObject wrapper, RectTransform card, CanvasGroup group,
            Image lifetimeProgress, float duration)
        {
            group.alpha = 0f;
            lifetimeProgress.fillAmount = 1f;
            yield return Tween(.3f, value =>
            {
                if (!card || !group) return;
                float eased = OutBack(value);
                card.anchoredPosition = new Vector2(Mathf.LerpUnclamped(-Width, 0f, eased), 0f);
                group.alpha = Mathf.Clamp01(value * 2f);
            });

            float visibleDuration = Mathf.Max(.25f, duration);
            float remaining = visibleDuration;
            while (remaining > 0f)
            {
                remaining -= Time.unscaledDeltaTime;
                if (lifetimeProgress)
                    lifetimeProgress.fillAmount = Mathf.Clamp01(remaining / visibleDuration);
                yield return null;
            }

            if (lifetimeProgress) lifetimeProgress.fillAmount = 0f;
            yield return Tween(.22f, value =>
            {
                if (!card || !group) return;
                float eased = value * value;
                card.anchoredPosition = new Vector2(Mathf.Lerp(0f, -Width, eased), 0f);
                group.alpha = 1f - value;
            });
            if (wrapper) Destroy(wrapper);
        }

        private static Image CreateLifetimeProgress(RectTransform parent, NotificationKind kind)
        {
            var trackObject = new GameObject("LifetimeProgressTrack", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            var trackRect = trackObject.GetComponent<RectTransform>();
            trackRect.SetParent(parent, false);
            trackRect.anchorMin = new Vector2(0f, 0f);
            trackRect.anchorMax = new Vector2(1f, 0f);
            trackRect.pivot = new Vector2(.5f, 0f);
            trackRect.anchoredPosition = new Vector2(0f, 3f);
            trackRect.sizeDelta = new Vector2(0f, 3f);
            var track = trackObject.GetComponent<Image>();
            track.color = new Color(.02f, .025f, .045f, .55f);
            track.raycastTarget = false;

            var fillObject = new GameObject("LifetimeProgress", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image), typeof(Outline));
            var fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.SetParent(trackRect, false);
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fill = fillObject.GetComponent<Image>();
            fill.color = ProgressColor(kind);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 1f;
            fill.raycastTarget = false;
            var glow = fillObject.GetComponent<Outline>();
            glow.effectColor = new Color(fill.color.r, fill.color.g, fill.color.b, .3f);
            glow.effectDistance = new Vector2(0f, 1f);
            return fill;
        }

        private static IEnumerator Tween(float duration, System.Action<float> update)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                update(Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            update(1f);
        }

        private static float OutBack(float value)
        {
            const float overshoot = 1.45f;
            float shifted = value - 1f;
            return 1f + (overshoot + 1f) * shifted * shifted * shifted + overshoot * shifted * shifted;
        }

        private static Color Background(NotificationKind kind) => kind switch
        {
            NotificationKind.Warning => new Color(.34f, .12f, .16f, .78f),
            NotificationKind.ItemAdded => new Color(.08f, .18f, .24f, .76f),
            NotificationKind.ItemRemoved => new Color(.17f, .12f, .22f, .76f),
            _ => new Color(.08f, .1f, .16f, .76f)
        };

        private static Color ProgressColor(NotificationKind kind) => kind switch
        {
            NotificationKind.Warning => new Color(1f, .48f, .4f, .95f),
            NotificationKind.ItemAdded => new Color(.25f, .88f, 1f, .95f),
            NotificationKind.ItemRemoved => new Color(.76f, .48f, 1f, .95f),
            _ => new Color(.5f, .7f, 1f, .95f)
        };

        private static void CreateText(string name, Transform parent, string value, Vector2 position,
            Vector2 size, int fontSize, TextAnchor alignment, Color color)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            var rect = textObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = textObject.GetComponent<Text>();
            text.font = SurvivalUITheme.Font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.text = value ?? string.Empty;
            text.raycastTarget = false;
        }
    }
}
