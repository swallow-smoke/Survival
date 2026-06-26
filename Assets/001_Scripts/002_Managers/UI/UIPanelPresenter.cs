using System.Collections.Generic;
using _001_Scripts.Base;
using _001_Scripts.Data.Message.Player;
using _001_Scripts.Type.States;
using MessagePipe;

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
    }
}
