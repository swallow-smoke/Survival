using UnityEngine;

namespace AstraNope.Core.World.Water
{
    [CreateAssetMenu(menuName = "Survival/Water/Physics Profile", fileName = "WaterPhysicsProfile")]
    public sealed class WaterPhysicsProfile : ScriptableObject
    {
        [Min(0f), SerializeField] private float density = 1000f;
        [Min(0f), SerializeField] private float linearDamping = 3f;
        [Min(0f), SerializeField] private float angularDamping = 2f;
        [Min(0f), SerializeField] private float flowDrag = 1.5f;
        [Min(0f), SerializeField] private float buoyancyMultiplier = 1f;
        [Min(0f), SerializeField] private float maximumForce = 100000f;

        public float Density => density;
        public float LinearDamping => linearDamping;
        public float AngularDamping => angularDamping;
        public float FlowDrag => flowDrag;
        public float BuoyancyMultiplier => buoyancyMultiplier;
        public float MaximumForce => maximumForce;
    }
}
