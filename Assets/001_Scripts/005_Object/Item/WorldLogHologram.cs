using UnityEngine;
using UnityEngine.UI;

namespace _001_Scripts.Structure
{
    [DisallowMultipleComponent]
    public sealed class WorldLogHologram : MonoBehaviour
    {
        private CanvasGroup _group;
        private Image _image;

        public void Configure(Sprite sprite)
        {
            EnsureView();
            _image.sprite = sprite;
            _image.preserveAspect = true;
            _image.color = sprite ? Color.white : new Color(.18f, .8f, 1f, .24f);
        }

        private void Awake() => EnsureView();

        private void EnsureView()
        {
            _group = GetComponent<CanvasGroup>();
            if (!_group) _group = gameObject.AddComponent<CanvasGroup>();
            var canvas = GetComponent<Canvas>();
            if (!canvas) canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 5;

            var rect = transform as RectTransform;
            if (!rect) return;
            rect.sizeDelta = new Vector2(180f, 180f);
            rect.localScale = Vector3.one * .0032f;

            var imageTransform = transform.Find("Image") as RectTransform;
            if (!imageTransform)
            {
                var imageObject = new GameObject("Image", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
                imageTransform = imageObject.GetComponent<RectTransform>();
                imageTransform.SetParent(transform, false);
                imageTransform.anchorMin = Vector2.zero;
                imageTransform.anchorMax = Vector2.one;
                imageTransform.offsetMin = imageTransform.offsetMax = Vector2.zero;
                var outline = imageObject.GetComponent<Outline>();
                outline.effectColor = new Color(.26f, .9f, 1f, .9f);
                outline.effectDistance = new Vector2(3f, -3f);
            }

            _image = imageTransform.GetComponent<Image>();
            _image.raycastTarget = false;
        }

        private void LateUpdate()
        {
            var camera = Camera.main;
            if (camera)
            {
                Vector3 direction = transform.position - camera.transform.position;
                if (direction.sqrMagnitude > .001f)
                    transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            }

            if (_group) _group.alpha = .72f + Mathf.Sin(Time.unscaledTime * 2.4f) * .18f;
        }
    }
}
