using System;
using _001_Scripts.Data;
using _001_Scripts.Data.Item;
using _001_Scripts.Data.Message;
using _001_Scripts.Type;
using MessagePipe;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using static _001_Scripts.Util;

namespace _001_Scripts.Controller
{
    [RequireComponent(typeof(InventoryController))]
    public class PlayerController : MonoBehaviour
    {
        private Animator _animator;
        IPublisher<InvMessage> _invMessagePublisher;
        IPublisher<CraftReqMessage> _craftMessagePublisher;
        IPublisher<PlayerStatMessage> _playerStatMessagePublisher;
        IPublisher<ForceWalkMessage> _forceWalkMessagePublisher;
        private PlayerState curState = PlayerState.Idle;
        private bool isRunning = false;
        private float _lastRun;

        private PlayerStatMessage postMsg;

        private IDisposable bag;

        [SerializeField] private PlayerStat stat;


        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        private void Update()
        {
            if (isRunning)
            {
                stat.ModifyStamina(-stat.GetStaminaUsage() * Time.deltaTime);
                stat.ModifyHungry(-stat.GetHungryUsage() * Time.deltaTime);
                stat.ModifyWater(-stat.GetWaterUsage() * Time.deltaTime);
                
                if (stat.GetStamina() <= 0)
                {
                    _forceWalkMessagePublisher.Publish(new ForceWalkMessage());
                    isRunning = false;
                }

                _lastRun = Time.time;
            }
            else if (Time.time - _lastRun >= 1f)
            {
                stat.ModifyStamina(Time.deltaTime * stat.GetStaminaCure());
            }
            
            
            PlayerStatMessage newMsg = new PlayerStatMessage(
                stat.GetHP(),
                stat.GetStamina(),
                stat.GetHungry(),
                stat.GetWater(),
                stat.GetOxygen()
            );

            if (HasSignificantChange(newMsg, postMsg))
            {
                _playerStatMessagePublisher.Publish(newMsg);
                postMsg = newMsg;
            }
        }

        public void OnRun(InputAction.CallbackContext ctx)
        {
            if (ctx.performed)
            {
                isRunning = true;
            }
            else
            {
                isRunning = false;
            }
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

        public void OnStateChange(StateMessage msg)
        {
            curState = msg.state;
        }

        [Inject]
        public void Construct(IPublisher<InvMessage> invMessagePublisher,
            IPublisher<CraftReqMessage> craftMessagePublisher,
            IPublisher<PlayerStatMessage> playerStatMessagePublisher,
            IPublisher<ForceWalkMessage> forceWalkMessagePublisher,
            ISubscriber<StateMessage> StateSubscriber)
        {
            var builder = DisposableBag.CreateBuilder();

            _invMessagePublisher = invMessagePublisher;
            _craftMessagePublisher = craftMessagePublisher;
            _playerStatMessagePublisher = playerStatMessagePublisher;
            _forceWalkMessagePublisher = forceWalkMessagePublisher;

            builder.Add(StateSubscriber.Subscribe(OnStateChange));
            bag = builder.Build();
        }

        private void OnDestroy()
        {
            bag?.Dispose();
        }
    }
}