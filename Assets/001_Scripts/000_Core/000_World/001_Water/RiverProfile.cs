using UnityEngine;

namespace _001_Scripts.Core._000_World._001_Water
{
    [CreateAssetMenu(menuName = "Survival/Water/River Profile", fileName = "RiverProfile")]
    public sealed class RiverProfile : ScriptableObject
    {
        [Min(0.1f), SerializeField] private float width = 8f;
        [Min(0.1f), SerializeField] private float depth = 3f;
        [Min(0f), SerializeField] private float flowSpeed = 2f;
        [Min(0.1f), SerializeField] private float sampleSpacing = 1f;
        [Min(0.001f), SerializeField] private float uvScale = 0.1f;
        [SerializeField] private Material material;
        [SerializeField] private WaterPhysicsProfile physicsProfile;

        public float Width => width;
        public float Depth => depth;
        public float FlowSpeed => flowSpeed;
        public float SampleSpacing => sampleSpacing;
        public float UvScale => uvScale;
        public Material Material => material;
        public WaterPhysicsProfile PhysicsProfile => physicsProfile;
    }
}
