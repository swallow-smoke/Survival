using System;
using System.Collections.Generic;
using AstraNope.UI.Base;
using AstraNope.Data.Messages.Player;
using AstraNope.Types.States;
using MessagePipe;
using UnityEngine;

namespace AstraNope.UI.Panels
{
    public class UIPanelPresenter
    {
        private readonly IReadOnlyDictionary<string, PanelBase> _panels;
        private readonly IPublisher<PlayerUIStateMessage> _statePublisher;

        public UIPanelPresenter(IReadOnlyDictionary<string, PanelBase> panels,
            IPublisher<PlayerUIStateMessage> statePublisher)
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
                _statePublisher.Publish(new PlayerUIStateMessage(PlayerUIState.None));
            }
            else
            {
                panel.Open();
                _statePublisher.Publish(new PlayerUIStateMessage(openState));
            }
        }

        public void ToggleExclusive(string key, PlayerUIState openState)
        {
            if (!_panels.TryGetValue(key, out var panel))
            {
                Debug.LogError($"UI panel is not registered: {key}");
                return;
            }

            if (panel.isViz)
            {
                panel.Close();
                _statePublisher.Publish(new PlayerUIStateMessage(PlayerUIState.None));
                return;
            }

            CloseOtherModalPanels(key);
            panel.Open();
            _statePublisher.Publish(new PlayerUIStateMessage(openState));
        }

        public void OpenExclusive(string key)
        {
            if (!_panels.TryGetValue(key, out var panel))
            {
                Debug.LogError($"UI panel is not registered: {key}");
                return;
            }
            if (!Enum.TryParse<PlayerUIState>(key, out var state))
            {
                Debug.LogError($"UI panel key does not match PlayerUIState: {key}");
                return;
            }

            CloseOtherModalPanels(key);
            if (!panel.isViz) panel.Open();
            _statePublisher.Publish(new PlayerUIStateMessage(state));
        }

        private void CloseOtherModalPanels(string exceptKey)
        {
            foreach (var pair in _panels)
            {
                if (pair.Key == exceptKey || !IsModalPanel(pair.Key)) continue;
                if (pair.Value && pair.Value.isViz) pair.Value.Close();
            }
        }

        public bool CloseAllModalPanels()
        {
            bool closedAny = false;
            foreach (var pair in _panels)
            {
                if (!IsModalPanel(pair.Key) || !pair.Value || !pair.Value.isViz) continue;
                pair.Value.Close();
                closedAny = true;
            }

            if (closedAny)
                _statePublisher.Publish(new PlayerUIStateMessage(PlayerUIState.None));
            return closedAny;
        }

        private static bool IsModalPanel(string key)
            => key == "Inventory" || key == "Log" || key == "Blueprint" || key == "Workbench" ||
               key == "SubmarineFabricator";

        public void Open(string key)
        {
            if (!_panels.TryGetValue(key, out var panel)) return;

            if (!Enum.TryParse<PlayerUIState>(key, out var state))
            {
                Debug.LogError($"UI panel key does not match PlayerUIState: {key}");
                return;
            }

            if (panel.isViz) return;

            panel.Open();
            _statePublisher.Publish(new PlayerUIStateMessage(state));
        }

        public void Close(string key)
        {
            if (!_panels.TryGetValue(key, out var panel)) return;

            if (!panel.isViz) return;

            panel.Close();
            _statePublisher.Publish(new PlayerUIStateMessage(PlayerUIState.None));
        }
    }
}
