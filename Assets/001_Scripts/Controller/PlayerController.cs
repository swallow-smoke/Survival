using System;
using System.Collections;
using _001_Scripts.Data;
using _001_Scripts.Data.Item;
using _001_Scripts.Data.Message;
using _001_Scripts.Type;
using _001_Scripts.Type.States;
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
        private IPublisher<InvMessage> _invMessagePublisher;
        private IPublisher<CraftReqMessage> _craftMessagePublisher;
        private IPublisher<PlayerStatMessage> _playerStatMessagePublisher;
        private float _lastRun;

        private PlayerStatMessage postMsg;
        private PlayerMovementState curState = PlayerMovementState.Idle;

        private IDisposable bag;

        [SerializeField] private PlayerStat stat;
        [SerializeField] private Animator animator;
        [SerializeField] private RuntimeAnimatorController movement;
        [SerializeField] private RuntimeAnimatorController falling;
        [SerializeField] AnimationClip LandClip;
        private Coroutine _landCoroutine;


        private void Update()
        {
            if (curState == PlayerMovementState.Running)
            {
                stat.ModifyStamina(-stat.GetStaminaUsage() * Time.deltaTime);
                stat.ModifyHungry(-stat.GetHungryUsage() * Time.deltaTime);
                stat.ModifyWater(-stat.GetWaterUsage() * Time.deltaTime);
                
                if (stat.GetStamina() <= 0)
                {
                    curState = PlayerMovementState.Walking;
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
                stat.GetOxygen(),
                stat.GetTemp()
            );

            // if (HasSignificantChange(newMsg, postMsg))
            // {
                _playerStatMessagePublisher.Publish(newMsg);
                postMsg = newMsg;
            // }
        }

        public void OnRun(InputAction.CallbackContext ctx)
        {
            if (ctx.performed) curState = PlayerMovementState.Running;
            else curState = PlayerMovementState.Walking;
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
                item,
                1
            );

            _invMessagePublisher.Publish(invMsg);
        }

        private void OnMove(PlayerMovementMessage msg)
        {
            if (msg.velocity < 0)
            {
                curState = PlayerMovementState.Idle;
            }
        }


        
        [Inject]
        public void Construct(IPublisher<InvMessage> invMessagePublisher,
            IPublisher<CraftReqMessage> craftMessagePublisher,
            IPublisher<PlayerStatMessage> playerStatMessagePublisher,
            ISubscriber<PlayerMovementMessage> movementSubscriber)
        {
            var builder = DisposableBag.CreateBuilder();

            _invMessagePublisher = invMessagePublisher;
            _craftMessagePublisher = craftMessagePublisher;
            _playerStatMessagePublisher = playerStatMessagePublisher;
            builder.Add(movementSubscriber.Subscribe(OnMove));

            bag = builder.Build();
        }

        private void OnDestroy()
        {
            bag?.Dispose();
        }
    }
}