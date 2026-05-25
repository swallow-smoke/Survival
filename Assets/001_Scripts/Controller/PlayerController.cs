using System;
using System.Collections;
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
        [SerializeField] private Animator animator;
        [SerializeField] private RuntimeAnimatorController movement;
        [SerializeField] private RuntimeAnimatorController falling;
        [SerializeField] AnimationClip LandClip;
        private bool isGrounded = true;
        private bool isLanded;
        private Coroutine _landCoroutine;


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
            if (ctx.performed) isRunning = true;
            else isRunning = false;
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

        public void OnStateChange(StateMessage msg) => curState = msg.state;

        private void OnAnimation(PlayerMovementMessage msg)
        {
            if (isGrounded != msg.isGround)
            {
                if (!msg.isGround)
                {
                    if (_landCoroutine != null) 
                        StopCoroutine(_landCoroutine);
                    animator.runtimeAnimatorController = falling;
                    isLanded = false;
                    animator.SetFloat("SpeedZ", msg.rawVector3.y);
                }
                else
                {
                    if (isLanded == false)
                    {
                        animator.SetBool("isGround", true);
                        _landCoroutine = StartCoroutine(LandCoroutine());
                        isLanded = true;
                    }
                }
            }
            else
            {
                Debug.Log($"착지 감지 - isLanded: {isLanded}");
                animator.SetBool("isGround", true);
                Debug.Log($"SetBool 호출됨 - 현재 컨트롤러: {animator.runtimeAnimatorController.name}");
                _landCoroutine = StartCoroutine(LandCoroutine());
                isLanded = true;
            }
            
            animator.SetFloat("SpeedX", msg.rawVector3.x);
            animator.SetFloat("SpeedY", msg.rawVector3.z);

            isGrounded = msg.isGround;
        }

        IEnumerator LandCoroutine()
        {
            yield return new WaitForSeconds(LandClip.length);
            animator.runtimeAnimatorController = movement;
            animator.SetFloat("SpeedX", 0f);
            animator.SetFloat("SpeedY", 0f);
            isLanded = false;
        }
        
        [Inject]
        public void Construct(IPublisher<InvMessage> invMessagePublisher,
            IPublisher<CraftReqMessage> craftMessagePublisher,
            IPublisher<PlayerStatMessage> playerStatMessagePublisher,
            IPublisher<ForceWalkMessage> forceWalkMessagePublisher,
            ISubscriber<StateMessage> StateSubscriber,
            ISubscriber<PlayerMovementMessage> movementSubscriber)
        {
            var builder = DisposableBag.CreateBuilder();

            _invMessagePublisher = invMessagePublisher;
            _craftMessagePublisher = craftMessagePublisher;
            _playerStatMessagePublisher = playerStatMessagePublisher;
            _forceWalkMessagePublisher = forceWalkMessagePublisher;

            builder.Add(StateSubscriber.Subscribe(OnStateChange));
            builder.Add(movementSubscriber.Subscribe(OnAnimation));
            bag = builder.Build();
        }

        private void OnDestroy()
        {
            bag?.Dispose();
        }
    }
}