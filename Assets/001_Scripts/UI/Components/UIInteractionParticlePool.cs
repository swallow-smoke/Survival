using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace AstraNope.UI.Components
{
    public sealed class UIInteractionParticlePool : MonoBehaviour
    {
        [SerializeField] private List<Image> particles = new();
        private int _next;

        public void Emit(Vector2 origin, int count, Color color, Sprite sprite)
        {
            if (particles.Count == 0) return;
            for (int i = 0; i < count; i++)
            {
                var image = particles[_next++ % particles.Count];
                if (!image) continue;
                image.DOKill();
                var rect = image.rectTransform;
                rect.DOKill();
                image.gameObject.SetActive(true);
                image.sprite = sprite;
                image.type = sprite ? Image.Type.Sliced : Image.Type.Simple;
                image.color = Color.Lerp(color, new Color(.78f, .52f, 1f, .95f), Random.value);
                float size = Random.Range(4f, 9f);
                rect.sizeDelta = new Vector2(size, size);
                rect.anchoredPosition = origin;
                rect.localScale = Vector3.one;
                float angle = (360f / Mathf.Max(1, count)) * i + Random.Range(-18f, 18f);
                float distance = Random.Range(24f, 48f);
                Vector2 destination = origin + new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad),
                    Mathf.Sin(angle * Mathf.Deg2Rad)) * distance;
                float duration = Random.Range(.28f, .44f);
                rect.DOAnchorPos(destination, duration).SetEase(Ease.OutCubic).SetUpdate(true);
                rect.DOScale(.15f, duration).SetEase(Ease.InQuad).SetUpdate(true);
                image.DOFade(0f, duration).SetEase(Ease.InQuad).SetUpdate(true)
                    .OnComplete(() => image.gameObject.SetActive(false));
            }
        }
    }
}
