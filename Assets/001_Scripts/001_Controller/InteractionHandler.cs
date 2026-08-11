using System;
using _001_Scripts.Controller.Handler;
using _001_Scripts.Controller.Interaction;
using _001_Scripts.Data.Message;
using _001_Scripts.Data.Message.Player;
using _001_Scripts.Data.Structure.Interface;
using _001_Scripts.Entities;
using _001_Scripts.Interface;
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
        private IInteractionTarget _current;
        private Transform _lastHitTrs;
        private bool _canInteract;
        private PlayerUIState _uiState;
        private IInputService _input;
        private IResourceInteractionService _resourceInteraction;
        private IDisposable _bag;
        private OutlineHighlighter _highlighter;
        private bool _hasResourceTarget;
        private string _resourceLabel;

        [Inject]
        public void Construct(IPublisher<InteractionUIMessage> uiPublisher,
            ISubscriber<PlayerUIStateMsg> uiStateSubscriber,
            IInputService inputService,
            IResourceInteractionService resourceInteraction)
        {
            _uiPublisher = uiPublisher;
            _input = inputService;
            _resourceInteraction = resourceInteraction;
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
            if (_uiState != PlayerUIState.None && (_lastHitTrs != null || _hasResourceTarget))
                ClearTarget();
        }

        private void ClearTarget()
        {
            if (_lastHitTrs != null) _highlighter.SetHighlight(_lastHitTrs.gameObject, false);
            _lastHitTrs = null;
            _current = null;
            _canInteract = false;
            _hasResourceTarget = false;
            _resourceLabel = null;
            _resourceInteraction?.ClearFocus();
            _uiPublisher.Publish(new InteractionUIMessage(false, "", "F"));
        }

        private void Update()
        {
            if (_uiState != PlayerUIState.None) return;

            RaycastHit hit;
            if (Physics.Raycast(_trs.position, _trs.forward, out hit, maxDistance, interactLayer))
            {
                _hasResourceTarget = false;
                _resourceInteraction?.ClearFocus();
                Transform hitTrs = hit.collider.transform;
                if (!ReferenceEquals(_lastHitTrs, hitTrs))
                {
                    if (_lastHitTrs != null)
                        _highlighter.SetHighlight(_lastHitTrs.gameObject, false);

                    _lastHitTrs = hitTrs;
                    _current = hitTrs.GetComponentInParent<IInteractionTarget>();
                    if (_current == null)
                    {
                        var entity = hitTrs.GetComponentInParent<Entity>();
                        if (entity) entity.TryGetFeature(out _current);
                    }

                    if (_current != null)
                        _highlighter.SetHighlight(hitTrs.gameObject, true);

                    string label;
                    if (_current is IConditionalInteractionTarget conditional)
                    {
                        _canInteract = conditional.CanInteract();
                        label = _canInteract
                            ? conditional.GetLabel()
                            : conditional.RequirementLabel();
                    }
                    else
                    {
                        _canInteract = true;
                        label = _current?.GetLabel() ?? "";
                    }

                    string promptKey = _current is IInteractionPrompt prompt ? prompt.GetPromptKey() : "F";
                    _uiPublisher.Publish(new InteractionUIMessage(_current != null, label, promptKey));
                }
            }
            else if (_resourceInteraction != null &&
                     _resourceInteraction.TryFocus(_trs.position, _trs.forward, maxDistance, out var resourceFocus))
            {
                if (_lastHitTrs != null)
                {
                    _highlighter.SetHighlight(_lastHitTrs.gameObject, false);
                    _lastHitTrs = null;
                    _current = null;
                }

                if (!_hasResourceTarget || _resourceLabel != resourceFocus.Label)
                {
                    _hasResourceTarget = true;
                    _resourceLabel = resourceFocus.Label;
                    _uiPublisher.Publish(new InteractionUIMessage(true, resourceFocus.Label, "F"));
                }
            }
            else if (_lastHitTrs != null || _hasResourceTarget)
            {
                ClearTarget();
            }
        }

        private void HandleInteract()
        {
            if (_uiState != PlayerUIState.None) return;
            if (_current is IConditionalInteractionTarget && !_canInteract) return;
            if (_current != null)
            {
                _current.Interact();
                return;
            }

            _resourceInteraction?.InteractFocused();
        }

        private void OnDestroy()
        {
            if (_input != null)
                _input.OnInteract -= HandleInteract;

            _bag?.Dispose();
        }
    }
}
