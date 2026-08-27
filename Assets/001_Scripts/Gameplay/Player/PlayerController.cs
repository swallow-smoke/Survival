using System;
using AstraNope.Gameplay.Input;
using AstraNope.Gameplay.Survival;
using AstraNope.Data;
using AstraNope.Data.Items;
using AstraNope.Data.Messages;
using AstraNope.WorldObjects.Entities;
using AstraNope.Types.States;
using MessagePipe;
using UnityEngine;
using VContainer;

using AstraNope.Contracts;
namespace AstraNope.Gameplay.Player
{
    [RequireComponent(typeof(InventoryController), typeof(Entity), typeof(Health))]
    [RequireComponent(typeof(Living))]
    public class PlayerController : MonoBehaviour, IItemUseTarget
    {
        private IPublisher<InventoryRequestMessage> _invMessagePublisher;
        private IInputService _input;

        private SurvivalStatSimulator _survival;
        private PlayerMovementState curState = PlayerMovementState.Idle;
        private bool isSwimming;

        private IDisposable bag;

        [SerializeField] private PlayerStat stat;
        private Health _health;

        private void Awake()
        {
            var entity = GetComponent<Entity>();
            if (!entity) entity = gameObject.AddComponent<Entity>();
            entity.SetKind(EntityKind.Player);
            _health = GetComponent<Health>();
            if (!_health) _health = gameObject.AddComponent<Health>();
            if (!GetComponent<Living>()) gameObject.AddComponent<Living>();
            _health.Changed += SyncEntityHealthToStat;
            SyncEntityHealthToStat();
        }

        private void Update()
        {
            if (_survival == null) return;
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
            var invMsg = new InventoryRequestMessage(
                InvMessageType.Added,
                item,
                1
            );

            _invMessagePublisher.Publish(invMsg);
        }

        public bool ApplyConsumable(Item item)
        {
            if (item == null || !item.TryGetFeature<IUsable>(out var usable)) return false;
            usable.Use(this);
            return true;
        }

        public void RestoreHealth(float amount) => _health.RestoreHealth(amount);
        public void ModifyOxygen(float amount) => stat.ModifyOxygen(amount);
        public void ModifyFood(float amount) => stat.ModifyHungry(amount);
        public void ModifyWater(float amount) => stat.ModifyWater(amount);

        private void SyncEntityHealthToStat()
        {
            if (stat == null) return;
            stat.SetHP(Mathf.RoundToInt(_health.HealthRatio * 100f));
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
        public void Construct(IPublisher<InventoryRequestMessage> invMessagePublisher,
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
            if (_health) _health.Changed -= SyncEntityHealthToStat;
            if (_input != null)
            {
                _input.OnRun -= HandleRun;
            }

            bag?.Dispose();
        }
    }
}
