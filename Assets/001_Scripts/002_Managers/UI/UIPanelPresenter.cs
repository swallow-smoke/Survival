using System;
using System.Collections.Generic;
using _001_Scripts.Base;
using _001_Scripts.Data.Message.Player;
using _001_Scripts.Type.States;
using MessagePipe;
using UnityEngine;

namespace _001_Scripts.Managers
{
    public class UIPanelPresenter
    {
        private readonly IReadOnlyDictionary<string, PanelBase> _panels;
        private readonly IPublisher<PlayerUIStateMsg> _statePublisher;

        public UIPanelPresenter(IReadOnlyDictionary<string, PanelBase> panels,
            IPublisher<PlayerUIStateMsg> statePublisher)
        {
            _panels = panels;
            _statePublisher = statePublisher;
        }

        public void Toggle(string key, PlayerUIState openState)
        {
            if (!_panels.TryGetValue(key, out var panel)) return;

            if (panel.isViz)
            {
                panel.Close();
                _statePublisher.Publish(new PlayerUIStateMsg(PlayerUIState.None));
            }
            else
            {
                panel.Open();
                _statePublisher.Publish(new PlayerUIStateMsg(openState));
            }
        }

        public void Open(string key)
        {
            if (!_panels.TryGetValue(key, out var panel)) return;

            if (!Enum.TryParse<PlayerUIState>(key, out var state))
            {
                Debug.LogError("panelKey와 enum 이름 불일치");
                return;
            }

            if (panel.isViz) return;

            panel.Open();
            _statePublisher.Publish(new PlayerUIStateMsg(state));
        }

        public void Close(string key)
        {
            if (!_panels.TryGetValue(key, out var panel)) return;

            if (!panel.isViz) return;

            panel.Close();
            _statePublisher.Publish(new PlayerUIStateMsg(PlayerUIState.None));
        }
    }
}
