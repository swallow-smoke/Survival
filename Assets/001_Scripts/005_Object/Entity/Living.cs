using UnityEngine;

namespace _001_Scripts.Entities
{
    [DisallowMultipleComponent]
    public sealed class Living : EntityFeature
    {
        [SerializeField, Min(0f)] private float metabolismMultiplier = 1f;
        [SerializeField] private bool canBreatheUnderwater;

        public float MetabolismMultiplier => metabolismMultiplier;
        public bool CanBreatheUnderwater => canBreatheUnderwater;
    }
}
