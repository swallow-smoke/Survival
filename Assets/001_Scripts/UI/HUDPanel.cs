using System;
using System.Collections.Generic;
using System.Threading;
using _001_Scripts.Base;
using _001_Scripts.Data.Message;
using _001_Scripts.UI.Component;
using MessagePipe;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _001_Scripts.UI
{
    public class HUDPanel : PanelBase
    {
        [SerializeField] private Stamina stamina;
        [SerializeField] private Image hp;
        [SerializeField] private Image hungry;
        [SerializeField] private Image water;

        private IDisposable _bag;
        
        [Inject]
        public void Constructor(ISubscriber<PlayerStatMessage> playerStatSubscriber)
        {
            var bag = playerStatSubscriber.Subscribe(UIUpdate);
        }

        public void UIUpdate(PlayerStatMessage msg)
        {
            hp.fillAmount = msg.hp;
            hungry.fillAmount = msg.hungry;
            water.fillAmount = msg.water;
            
            stamina.StatUpdate(msg.stamina);
        }

        private void OnDestroy()
        {
            base.OnDestroy(); 
            _bag.Dispose();
        }
    }
}