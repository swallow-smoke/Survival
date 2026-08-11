using UnityEngine;

namespace _001_Scripts.Structure
{
    [DisallowMultipleComponent]
    public sealed class ResourceNodeHitFX : MonoBehaviour
    {
        [SerializeField] private ParticleSystem hitParticles;

        public void Play()
        {
            if (!hitParticles) return;
            hitParticles.transform.rotation = Random.rotation;
            hitParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            hitParticles.Play(true);
        }
    }
}
