using System;
using _001_Scripts.Base;
using _001_Scripts.Data.Message;
using MessagePipe;
using TMPro;
using UnityEngine;
using VContainer;

namespace _001_Scripts.UI
{
    public class InteractionPanel : PanelBase
    {
        [SerializeField] private TMP_Text labelText;

        private IDisposable _bag;

        [Inject]
        public void Constructor(ISubscriber<InteractionUIMessage> subscriber)
        {
            var builder = DisposableBag.CreateBuilder();
            builder.Add(subscriber.Subscribe(OnMessage));
            _bag = builder.Build();
        }

        private void OnMessage(InteractionUIMessage msg)
        {
            if (msg.isVisible)
            {
                labelText.text = msg.label;
                if (!isViz) Open();
            }
            else
            {
                if (isViz) Close();
            }
        }

        private void OnDestroy()
        {
            _bag?.Dispose();
        }
    }
}
