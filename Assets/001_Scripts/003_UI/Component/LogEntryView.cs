using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace _001_Scripts.UI.Component
{
    public sealed class LogEntryView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Text title;
        private UnityAction _listener;

        public void Show(int index, string value, Action<int> selected)
        {
            gameObject.SetActive(true);
            if (title) title.text = value;
            if (button)
            {
                if (_listener != null) button.onClick.RemoveListener(_listener);
                _listener = () => selected(index);
                button.onClick.AddListener(_listener);
            }
        }

        public void SetSelected(bool selected)
        {
            if (button && button.targetGraphic)
                button.targetGraphic.color = selected
                    ? new Color(.31f, .20f, .52f, .80f)
                    : new Color(.13f, .08f, .23f, .74f);
        }
    }
}
