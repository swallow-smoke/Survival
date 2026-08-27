using System;
using AstraNope.Data.Messages;
using MessagePipe;
using UnityEngine;
using UnityEngine.UI;

namespace AstraNope.UI.Components
{
    public sealed class ModalNavigation : MonoBehaviour
    {
        [SerializeField] private string panelKey;
        [SerializeField] private Button inventoryButton;
        [SerializeField] private Button logButton;
        [SerializeField] private Button blueprintButton;
        [SerializeField] private Button closeButton;
        private IPublisher<UIReqMessage> _publisher;
        private bool _bound;

        public void Bind(IPublisher<UIReqMessage> publisher)
        {
            _publisher = publisher;
            if (_bound) return;
            if (inventoryButton) inventoryButton.onClick.AddListener(() => Open("Inventory"));
            if (logButton) logButton.onClick.AddListener(() => Open("Log"));
            if (blueprintButton) blueprintButton.onClick.AddListener(() => Open("Blueprint"));
            if (closeButton) closeButton.onClick.AddListener(Close);
            _bound = true;
        }

        private void Open(string key) =>
            _publisher?.Publish(new UIReqMessage(UIReqMsgType.Open, key));

        private void Close() =>
            _publisher?.Publish(new UIReqMessage(UIReqMsgType.Close, panelKey));
    }
}
