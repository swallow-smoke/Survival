using _001_Scripts.Object.Vehicle;
using _001_Scripts.Type.States;
using UnityEngine;

namespace _001_Scripts.Entities
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    public sealed class Vehicle : EntityFeature, IVehicle
    {
        [SerializeField, Min(0f)] private float fuel = 100f;
        [SerializeField, Min(0f)] private float maxFuel = 100f;

        private Health _health;

        public float Fuel => fuel;
        public float MaxFuel => maxFuel;
        public VehicleConditionState Condition { get; private set; } = VehicleConditionState.Normal;

        protected override void Awake()
        {
            base.Awake();
            Owner.SetKind(EntityKind.Vehicle);
            _health = GetComponent<Health>();
            if (!_health) _health = gameObject.AddComponent<Health>();
            maxFuel = Mathf.Max(0f, maxFuel);
            fuel = Mathf.Clamp(fuel, 0f, maxFuel);
            _health.Changed += RefreshCondition;
            _health.Died += RefreshCondition;
            RefreshCondition();
        }

        protected override void OnDisable()
        {
            if (_health)
            {
                _health.Changed -= RefreshCondition;
                _health.Died -= RefreshCondition;
            }
            base.OnDisable();
        }

        public bool ConsumeFuel(float amount)
        {
            if (!_health.IsAlive || amount <= 0f || fuel < amount) return false;
            fuel -= amount;
            return true;
        }

        public void Refuel(float amount)
        {
            if (!_health.IsAlive || amount <= 0f) return;
            fuel = Mathf.Clamp(fuel + amount, 0f, maxFuel);
        }

        public void Repair(float amount) => _health.RestoreHealth(amount);
        public void RestoreHealth(float amount) => _health.RestoreHealth(amount);

        private void RefreshCondition()
        {
            if (!_health || !_health.IsAlive) Condition = VehicleConditionState.Destroyed;
            else Condition = _health.HealthRatio <= .5f
                ? VehicleConditionState.Damaged
                : VehicleConditionState.Normal;
        }
    }
}
