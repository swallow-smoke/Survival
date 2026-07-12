using _001_Scripts.Data.Message;
using _001_Scripts.Data.Structure.Interface;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace _001_Scripts.Structure
{
    public class CraftStationInteractable : MonoBehaviour, IInteractable, IInteractableInfo
    {
        [SerializeField] private string panelKey = "Craft";
        [SerializeField] private string displayLabel = "Craft";

        private IPublisher<UIReqMessage> _uiPublisher;

        [Inject]
        public void Construct(IPublisher<UIReqMessage> uiPublisher)
        {
            _uiPublisher = uiPublisher;
        }

        public void Interact()
        {
            _uiPublisher.Publish(new UIReqMessage(UIReqMsgType.Open, panelKey));
        }

        public string GetLabel() => displayLabel;
    }
}
