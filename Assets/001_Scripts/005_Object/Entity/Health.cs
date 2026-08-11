using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace _001_Scripts.Entities
{
    [DisallowMultipleComponent]
    public sealed class Health : EntityFeature, IDamageable, IRepairable
    {
        [FormerlySerializedAs("maxHP")]
        [SerializeField, Min(1f)] private float maxHealth = 100f;
        [SerializeField, Min(0f)] private float currentHealth = 100f;
        [SerializeField] private bool destroyEntityOnDeath = true;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public float HealthRatio => maxHealth <= 0f ? 0f : currentHealth / maxHealth;
        public bool IsAlive => currentHealth > 0f;

        public event Action<float> Damaged;
        public event Action<float> Healed;
        public event Action Changed;
        public event Action Died;

        protected override void Awake()
        {
            base.Awake();
            maxHealth = Mathf.Max(1f, maxHealth);
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
            if (currentHealth <= 0f) currentHealth = maxHealth;
        }

        public void ApplyDamage(float amount)
        {
            if (!IsAlive || amount <= 0f) return;
            float applied = Mathf.Min(amount, currentHealth);
            currentHealth -= applied;
            Damaged?.Invoke(applied);
            Changed?.Invoke();
            if (currentHealth > 0f) return;
            Died?.Invoke();
            if (destroyEntityOnDeath) Destroy(Owner.gameObject);
        }

        public void RestoreHealth(float amount)
        {
            if (!IsAlive || amount <= 0f || currentHealth >= maxHealth) return;
            float restored = Mathf.Min(amount, maxHealth - currentHealth);
            currentHealth += restored;
            Healed?.Invoke(restored);
            Changed?.Invoke();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            maxHealth = Mathf.Max(1f, maxHealth);
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        }
#endif
    }
}
