using UnityEngine;

namespace _001_Scripts.Controller.Interaction
{
    [DisallowMultipleComponent]
    public sealed class ResourceHitParticlePool : MonoBehaviour
    {
        [SerializeField] private ParticleSystem[] particles;
        private int nextIndex;

        public void Play(Vector3 worldPosition)
        {
            if (particles == null || particles.Length == 0) return;
            for (int offset = 0; offset < particles.Length; offset++)
            {
                int index = (nextIndex + offset) % particles.Length;
                ParticleSystem particle = particles[index];
                if (!particle || particle.isPlaying) continue;
                nextIndex = (index + 1) % particles.Length;
                particle.transform.SetPositionAndRotation(worldPosition, Random.rotation);
                particle.Play(true);
                return;
            }

            ParticleSystem fallback = particles[nextIndex];
            nextIndex = (nextIndex + 1) % particles.Length;
            if (!fallback) return;
            fallback.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            fallback.transform.SetPositionAndRotation(worldPosition, Random.rotation);
            fallback.Play(true);
        }
    }
}
