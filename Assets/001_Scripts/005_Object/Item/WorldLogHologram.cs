using UnityEngine;
using UnityEngine.UI;

namespace _001_Scripts.Structure
{
    [DisallowMultipleComponent]
    public sealed class WorldLogHologram : MonoBehaviour
    {
        [SerializeField, Tooltip("Scene-authored CanvasGroup used for the hologram pulse.")]
        private CanvasGroup group;

        [SerializeField, Tooltip("Drop the log image UI object here. Its Sprite can be replaced directly or by Logs.json.")]
        private Image image;

        public Image ImageSlot => image;

        public void ConfigureView(CanvasGroup canvasGroup, Image imageSlot)
        {
            group = canvasGroup;
            image = imageSlot;
            ConfigureCanvas();
        }

        public void Configure(Sprite sprite)
        {
            EnsureView();
            if (!image) return;
            // A JSON resource overrides the scene slot. If the JSON field is empty,
            // keep the Sprite the designer assigned directly in the scene.
            if (sprite) image.sprite = sprite;
            image.preserveAspect = true;
            image.color = image.sprite ? Color.white : new Color(.18f, .8f, 1f, .24f);
        }

        private void Awake() => EnsureView();

        private void EnsureView()
        {
            if (!group) group = GetComponent<CanvasGroup>();
            if (!image) image = GetComponentInChildren<Image>(true);
            ConfigureCanvas();

            if (!group || !image)
                Debug.LogWarning("[WorldLogHologram] Assign a scene-authored CanvasGroup and Image slot.", this);
        }

        private void ConfigureCanvas()
        {
            var canvas = GetComponent<Canvas>();
            if (!canvas) return;
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 5;

            var rect = transform as RectTransform;
            if (!rect) return;
            rect.sizeDelta = new Vector2(180f, 180f);
            rect.localScale = Vector3.one * .0032f;
            if (image) image.raycastTarget = false;
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

            if (group) group.alpha = .72f + Mathf.Sin(Time.unscaledTime * 2.4f) * .18f;
        }
    }
}
