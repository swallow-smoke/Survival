using System.Linq;
using _001_Scripts.Data.Message;
using _001_Scripts.Data.Structure.Interface;
using MessagePipe;
using UnityEngine;
using UnityEngine.InputSystem;
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

        [Inject]
        public void Construct(IPublisher<InteractionUIMessage> uiPublisher)
        {
            _uiPublisher = uiPublisher;
        }

        private void SetHighlight(GameObject go, bool on)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) return;

            var mats = renderer.materials.ToList();

            if (on)
            {
                if (!mats.Any(m => m.shader == _outlineMat.shader))
                    mats.Add(_outlineMat);
            }
            else
            {
                mats.RemoveAll(m => m.shader == _outlineMat.shader);
            }

            renderer.materials = mats.ToArray();
        }

        private void Update()
        {
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
                SetHighlight(_lastHitTrs.gameObject, false);

                _lastHitTrs = null;
                _current = null;
                _canInteract = false;
                _uiPublisher.Publish(new InteractionUIMessage(false, "", "F"));
            }
        }

        public void OnInteract(InputAction.CallbackContext context)
        {
            if (!context.started) return;
            if (_current is IConditionalInteractable && !_canInteract) return;
            _current?.Interact();
        }
    }
}