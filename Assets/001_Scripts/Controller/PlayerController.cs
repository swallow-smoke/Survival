using _001_Scripts.Data.Item;
using _001_Scripts.Data.Message;
using _001_Scripts.Type;
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
        private PlayerState curState = PlayerState.Idle;
        
        
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

        public void OnStateChange(PlayerStateMessage msg)
        {
            curState = msg.state;
        }

        [Inject]
        public void Constructor(IPublisher<InvMessage> invMessagePublisher, 
            IPublisher<CraftReqMessage> craftMessagePublisher, 
            ISubscriber<PlayerStateMessage> playerStateSubscriber)
        {
            var bag = DisposableBag.CreateBuilder();
            
            _invMessagePublisher = invMessagePublisher;
            _craftMessagePublisher = craftMessagePublisher;
            playerStateSubscriber.Subscribe(msg => OnStateChange(msg));
        }
    }
}