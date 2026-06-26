using System;
using _001_Scripts.Controller.Handler;
using _001_Scripts.Controller.Survival;
using _001_Scripts.Data;
using _001_Scripts.Data.Message;
using _001_Scripts.Type.States;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace _001_Scripts.Controller
{
    [RequireComponent(typeof(InventoryController))]
    public class PlayerController : MonoBehaviour
    {
        private IPublisher<InvReqMessage> _invMessagePublisher;
        private IInputService _input;

        private SurvivalStatSimulator _survival;
        private PlayerMovementState curState = PlayerMovementState.Idle;
        private bool isSwimming;

        private IDisposable bag;

        [SerializeField] private PlayerStat stat;

        private void Update()
        {
            bool staminaDepleted = _survival.Tick(curState == PlayerMovementState.Running, Time.deltaTime, Time.time);
            if (staminaDepleted)
                curState = PlayerMovementState.Walking;
        }

        private void HandleRun(bool value)
        {
            if (isSwimming) return;
            curState = value ? PlayerMovementState.Running : PlayerMovementState.Walking;
        }

        public void OnGetItem(int item)
        {
            var invMsg = new InvReqMessage(
                InvMessageType.Added,
                item,
                1
            );

            _invMessagePublisher.Publish(invMsg);
        }

        private void OnMove(PlayerMovementMessage msg)
        {
            isSwimming = msg.isSwimming;

            if (msg.isSwimming)
            {
                curState = PlayerMovementState.Swimming;
                return;
            }

            if (msg.velocity < 0)
            {
                curState = PlayerMovementState.Idle;
            }
        }

        private void Start()
        {
            if (_input == null) return;

            _input.OnRun += HandleRun;
        }

        [Inject]
        public void Construct(IPublisher<InvReqMessage> invMessagePublisher,
            IPublisher<PlayerStatMessage> playerStatMessagePublisher,
            ISubscriber<PlayerMovementMessage> movementSubscriber,
            IInputService inputService)
        {
            var builder = DisposableBag.CreateBuilder();

            _invMessagePublisher = invMessagePublisher;
            _input = inputService;
            _survival = new SurvivalStatSimulator(stat, playerStatMessagePublisher);
            builder.Add(movementSubscriber.Subscribe(OnMove));

            bag = builder.Build();
        }

        private void OnDestroy()
        {
            if (_input != null)
            {
                _input.OnRun -= HandleRun;
            }

            bag?.Dispose();
        }
    }
}
