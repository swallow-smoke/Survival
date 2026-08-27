using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace AstraNope.Core.World.Entities.Creatures.AI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Survival/Creatures/Ray AI")]
    public sealed class RayAIAuthoring : MonoBehaviour
    {
        [Range(0f, 45f), SerializeField] private float maximumBankDegrees = 18f;
        [Min(0f), SerializeField] private float bankResponsiveness = 4f;

        private sealed class Baker : Baker<RayAIAuthoring>
        {
            public override void Bake(RayAIAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new RayAI
                {
                    MaximumBankRadians = math.radians(Mathf.Clamp(authoring.maximumBankDegrees, 0f, 45f)),
                    BankResponsiveness = Mathf.Max(0f, authoring.bankResponsiveness)
                });
            }
        }
    }
}
