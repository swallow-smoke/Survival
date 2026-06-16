using System;
using System.Collections.Generic;
using System.Threading;
using _001_Scripts.Base;
using _001_Scripts.Data.Message;
using _001_Scripts.Interface;
using _001_Scripts.UI.Component;
using MessagePipe;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _001_Scripts.UI
{
    public class HUDPanel : PanelBase
    {
        [SerializeField] private Image hp;
        [SerializeField] private Image hungry;
        [SerializeField] private Image water;
        [SerializeField] private Image oxygen;
        [SerializeField] private Image stamina;
        [SerializeField] private Image temp;

        private IDisposable _bag;

        [Inject]
        public void Constructor(ISubscriber<PlayerStatMessage> playerStatSubscriber)
        {
            var builder = DisposableBag.CreateBuilder();
            builder.Add(playerStatSubscriber.Subscribe(UIUpdate));

            _bag = builder.Build();
        }

        private void UIUpdate(PlayerStatMessage msg)
        {
            hp.fillAmount = (float)msg.hp / 100;
            hungry.fillAmount = msg.hungry / 100;
            water.fillAmount = msg.water / 100;
            oxygen.fillAmount = msg.oxygen / 100;
            stamina.fillAmount = msg.stamina / 100;
            temp.color = Color.Lerp(Color.blue, Color.red, msg.temp / 100);
        }

        private void OnDestroy()
        {
            _bag?.Dispose();
        }
    }
}