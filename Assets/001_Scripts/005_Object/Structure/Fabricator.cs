using _001_Scripts.Data.Message;
using _001_Scripts.Data.Structure.Interface;
using _001_Scripts.Entities;
using _001_Scripts.Managers;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace _001_Scripts.Structure
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(_001_Scripts.Entities.Structure))]
    public class Fabricator : EntityFeature, IInteractionTarget, IInteractionPrompt
    {
        [SerializeField] private string panelKey = "Workbench";
        [SerializeField] private string displayLabel = "제작대 사용";
        [SerializeField] private string promptKey = "LMB";

        private IPublisher<UIReqMessage> _uiPublisher;

        public string PanelKey => panelKey;

        protected override void Awake()
        {
            base.Awake();
            if (!GetComponent<_001_Scripts.Entities.Structure>())
                gameObject.AddComponent<_001_Scripts.Entities.Structure>();
        }

        [Inject]
        public void Construct(IPublisher<UIReqMessage> uiPublisher) => _uiPublisher = uiPublisher;

        public void Configure(string targetPanelKey, string label, string key = "LMB")
        {
            panelKey = targetPanelKey;
            displayLabel = label;
            promptKey = key;
        }

        public virtual void Interact()
        {
            BeforePanelOpen();
            if (_uiPublisher != null)
            {
                _uiPublisher.Publish(new UIReqMessage(UIReqMsgType.Open, panelKey));
                return;
            }

            var uiManager = FindAnyObjectByType<UIManager>();
            if (uiManager) uiManager.OpenPanel(panelKey);
            else Debug.LogError($"[{GetType().Name}] UIManager is unavailable.", this);
        }

        protected virtual void BeforePanelOpen() { }
        public string GetLabel() => displayLabel;
        public string GetPromptKey() => string.IsNullOrWhiteSpace(promptKey) ? "LMB" : promptKey;
    }
}
