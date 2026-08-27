using AstraNope.Data.Messages;
using AstraNope.Contracts.WorldObjects;
using AstraNope.WorldObjects.Entities;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace AstraNope.WorldObjects.Structures
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AstraNope.WorldObjects.Entities.Structure))]
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
            if (!GetComponent<AstraNope.WorldObjects.Entities.Structure>())
                gameObject.AddComponent<AstraNope.WorldObjects.Entities.Structure>();
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
            if (_uiPublisher == null)
            {
                Debug.LogError($"[{GetType().Name}] UI publisher unavailable.", this);
                return;
            }
            _uiPublisher.Publish(new UIReqMessage(UIReqMsgType.Open, panelKey));
        }

        protected virtual void BeforePanelOpen() { }
        public string GetLabel() => displayLabel;
        public string GetPromptKey() => string.IsNullOrWhiteSpace(promptKey) ? "LMB" : promptKey;
    }
}
