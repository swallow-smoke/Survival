using _001_Scripts.Data.Item;
using _001_Scripts.Data.Message;
using MessagePipe;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace _001_Scripts.Controller
{
    [RequireComponent(typeof(InventoryController))]
    public class PlayerController : MonoBehaviour
    {
        private Animator _animator;
        IPublisher<InvMessage> _invMessagePublisher;
        IPublisher<CraftReqMessage> _craftMessagePublisher;
        
        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        public void OnInteract(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                RaycastHit hit;
                if (Physics.Raycast(transform.position, transform.forward, out hit, 2.0f))
                {
                    Debug.Log("Interacted with: " + hit.collider.name);
                }
            }
        }

        public void OnGetItem(Item item)
        {
            var invMsg = new InvMessage(
                InvMessageType.Added,
                item
                );
            
            _invMessagePublisher.Publish(invMsg);
        }

        [Inject]
        public void Constructor(IPublisher<InvMessage> invMessagePublisher, IPublisher<CraftReqMessage> craftMessagePublisher)
        {
            _invMessagePublisher = invMessagePublisher;
            _craftMessagePublisher = craftMessagePublisher;
        }
    }
}