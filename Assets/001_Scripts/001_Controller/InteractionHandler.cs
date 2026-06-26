using System;
using _001_Scripts.Controller.Handler;
using _001_Scripts.Controller.Interaction;
using _001_Scripts.Data.Message;
using _001_Scripts.Data.Message.Player;
using _001_Scripts.Data.Structure.Interface;
using _001_Scripts.Type.States;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace _001_Scripts.Controller
{
    /// <summary>
    /// 시선 레이캐스트로 상호작용 대상을 감지하고, 라벨 UI를 발행한다.
    /// 하이라이트(머티리얼 조작)는 <see cref="OutlineHighlighter"/>에 위임한다.
    /// </summary>
    public class InteractionHandler : MonoBehaviour
    {
        [SerializeField] private Transform _trs;
        [SerializeField] private float maxDistance = 2.0f;
        [SerializeField] private LayerMask interactLayer;
        [SerializeField] private Material _outlineMat;

        private IPublisher<InteractionUIMessage> _uiPublisher;
        private IInteractable _current;
        private Transform _lastHitTrs;
        private bool _canInteract;
        private PlayerUIState _uiState;
        private IInputService _input;
        private IDisposable _bag;
        private OutlineHighlighter _highlighter;

        [Inject]
        public void Construct(IPublisher<InteractionUIMessage> uiPublisher,
            ISubscriber<PlayerUIStateMsg> uiStateSubscriber,
            IInputService inputService)
        {
            _uiPublisher = uiPublisher;
            _input = inputService;
            _highlighter = new OutlineHighlighter(_outlineMat);
            var builder = DisposableBag.CreateBuilder();
            builder.Add(uiStateSubscriber.Subscribe(OnUIStateChanged));
            _bag = builder.Build();
        }

        private void Start()
        {
            if (_input == null) return;

            _input.OnInteract += HandleInteract;
        }

        private void OnUIStateChanged(PlayerUIStateMsg msg)
        {
            _uiState = msg.state;
            if (_uiState != PlayerUIState.None && _lastHitTrs != null)
                ClearTarget();
        }

        private void ClearTarget()
        {
            _highlighter.SetHighlight(_lastHitTrs.gameObject, false);
            _lastHitTrs = null;
            _current = null;
            _canInteract = false;
            _uiPublisher.Publish(new InteractionUIMessage(false, "", "F"));
        }

        private void Update()
        {
            if (_uiState != PlayerUIState.None) return;

            RaycastHit hit;
            if (Physics.Raycast(_trs.position, _trs.forward, out hit, maxDistance, interactLayer))
            {
                Transform hitTrs = hit.collider.transform;
                if (!ReferenceEquals(_lastHitTrs, hitTrs))
                {
                    if (_lastHitTrs != null)
                        _highlighter.SetHighlight(_lastHitTrs.gameObject, false);

                    _lastHitTrs = hitTrs;
                    _current = hitTrs.GetComponent<IInteractable>();

                    if (_current != null)
                        _highlighter.SetHighlight(hitTrs.gameObject, true);

                    string label;
                    if (_current is IConditionalInteractable conditional)
                    {
                        _canInteract = conditional.CanInteract();
                        label = _canInteract
                            ? (_current is IInteractableInfo condInfo ? condInfo.GetLabel() : "")
                            : conditional.RequirementLabel();
                    }
                    else
                    {
                        _canInteract = true;
                        label = _current is IInteractableInfo info ? info.GetLabel() : "";
                    }

                    _uiPublisher.Publish(new InteractionUIMessage(_current != null, label, "F"));
                }
            }
            else if (_lastHitTrs != null)
            {
                ClearTarget();
            }
        }

        private void HandleInteract()
        {
            if (_uiState != PlayerUIState.None) return;
            if (_current is IConditionalInteractable && !_canInteract) return;
            _current?.Interact();
        }

        private void OnDestroy()
        {
            if (_input != null)
                _input.OnInteract -= HandleInteract;

            _bag?.Dispose();
        }
    }
}