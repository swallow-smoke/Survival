using System;
using _001_Scripts.Controller.Handler;
using _001_Scripts.Data.Message;
using _001_Scripts.Data.Message.Player;
using _001_Scripts.Data.Structure.Interface;
using _001_Scripts.Type.States;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace _001_Scripts.Controller
{
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

        [Inject]
        public void Construct(IPublisher<InteractionUIMessage> uiPublisher,
            ISubscriber<PlayerUIStateMsg> uiStateSubscriber,
            IInputService inputService)
        {
            _uiPublisher = uiPublisher;
            _input = inputService;
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
            SetHighlight(_lastHitTrs.gameObject, false);
            _lastHitTrs = null;
            _current = null;
            _canInteract = false;
            _uiPublisher.Publish(new InteractionUIMessage(false, "", "F"));
        }

        private void SetHighlight(GameObject go, bool on)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) return;

            var current = renderer.sharedMaterials;

            if (on)
            {
                if (current.Length > 0 && current[current.Length - 1] == _outlineMat)
                    return; // already highlighted

                var withOutline = new Material[current.Length + 1];
                current.CopyTo(withOutline, 0);
                withOutline[current.Length] = _outlineMat;
                renderer.sharedMaterials = withOutline;
            }
            else
            {
                if (current.Length == 0 || current[current.Length - 1] != _outlineMat)
                    return; // not highlighted

                var withoutOutline = new Material[current.Length - 1];
                Array.Copy(current, withoutOutline, current.Length - 1);
                renderer.sharedMaterials = withoutOutline;
            }
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
                        SetHighlight(_lastHitTrs.gameObject, false);

                    _lastHitTrs = hitTrs;
                    _current = hitTrs.GetComponent<IInteractable>();

                    if (_current != null)
                        SetHighlight(hitTrs.gameObject, true);

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