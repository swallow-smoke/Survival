using _001_Scripts.Data;
using UnityEngine;
using UnityEngine.UI;

namespace _001_Scripts.UI.Component
{
    public sealed class BlueprintTileView : MonoBehaviour
    {
        [SerializeField] private int blueprintId;
        [SerializeField] private Text nameLabel;
        [SerializeField] private Text glyphLabel;
        [SerializeField] private Text progressLabel;
        [SerializeField] private Image disc;
        [SerializeField] private Image icon;
        [SerializeField] private RectTransform progressFill;

        public int BlueprintId => blueprintId;

        public void Show(BlueprintUnlockStatus status)
        {
            gameObject.SetActive(true);
            if (nameLabel) nameLabel.text = status.Name;
            if (glyphLabel) glyphLabel.text = Glyph(status.CategoryPath);
            if (progressLabel)
            {
                progressLabel.gameObject.SetActive(!status.IsUnlocked);
                progressLabel.text = $"( {status.Progress} / {status.Required} )";
            }
            if (disc) disc.color = status.IsUnlocked
                ? new Color(.16f, .43f, .72f, .92f)
                : new Color(.23f, .22f, .30f, .88f);
            if (progressFill)
            {
                progressFill.parent.gameObject.SetActive(!status.IsUnlocked);
                progressFill.anchorMax = new Vector2((float)status.Progress / status.Required, 1f);
            }
            if (icon)
            {
                var sprite = string.IsNullOrWhiteSpace(status.IconResource)
                    ? null : Resources.Load<Sprite>(status.IconResource);
                icon.gameObject.SetActive(sprite);
                icon.sprite = sprite;
                if (glyphLabel) glyphLabel.gameObject.SetActive(!sprite);
            }
        }

        private static string Glyph(string category)
        {
            if (category.IndexOf("Vehicle", System.StringComparison.OrdinalIgnoreCase) >= 0) return "◉";
            if (category.IndexOf("Structure", System.StringComparison.OrdinalIgnoreCase) >= 0) return "▦";
            if (category.IndexOf("Material", System.StringComparison.OrdinalIgnoreCase) >= 0) return "◆";
            return "◇";
        }
    }
}
