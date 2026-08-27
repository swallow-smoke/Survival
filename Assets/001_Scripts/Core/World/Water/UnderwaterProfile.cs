using UnityEngine;

namespace AstraNope.Core.World.Water
{
    [CreateAssetMenu(menuName = "Survival/Water/Underwater Profile", fileName = "UnderwaterProfile")]
    public sealed class UnderwaterProfile : ScriptableObject
    {
        [SerializeField] private Color tint = new Color(0.15f, 0.55f, 0.65f, 1f);
        [Range(0f, 1f), SerializeField] private float saturation = 0.65f;
        [Range(0f, 1f), SerializeField] private float vignette = 0.2f;
        [Min(0.01f), SerializeField] private float transitionDuration = 0.35f;
        [Min(0f), SerializeField] private float fogDensity = 0.03f;
        [Min(0f), SerializeField] private float distortionStrength;
        [SerializeField] private Texture2D caustics;
        [Min(0f), SerializeField] private float causticsStrength;

        public Color Tint => tint;
        public float Saturation => saturation;
        public float Vignette => vignette;
        public float TransitionDuration => transitionDuration;
        public float FogDensity => fogDensity;
        public float DistortionStrength => distortionStrength;
        public Texture2D Caustics => caustics;
        public float CausticsStrength => causticsStrength;
    }
}
