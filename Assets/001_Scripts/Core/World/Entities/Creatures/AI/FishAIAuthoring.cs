using Unity.Entities;
using UnityEngine;

namespace AstraNope.Core.World.Entities.Creatures.AI
{
    public sealed class FishAIAuthoring : MonoBehaviour
    {
        [Header("Movement")] [SerializeField, Min(0f)]
        private float moveSpeed = 2f;

        [SerializeField, Min(0f)] private float fleeSpeed = 5f;
        [SerializeField, Min(0f)] private float turnSpeed = 3f;

        [Header("Detection")] [SerializeField, Min(0f)]
        private float fleeDistance = 5f;

        [Header("Wander")] [SerializeField, Min(0f)]
        private float wanderRadius = 8f;

        [SerializeField, Min(0.1f)] private float minWanderTime = 2f;
        [SerializeField, Min(0.1f)] private float maxWanderTime = 5f;

        private sealed class Baker : Baker<FishAIAuthoring>
        {
            public override void Bake(FishAIAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                
                AddComponent(entity, new FishAI
                {
                    MoveSpeed = authoring.moveSpeed,
                    FleeSpeed = authoring.fleeSpeed,
                    TurnSpeed = authoring.turnSpeed,
                    
                    FleeDistance = authoring.fleeDistance,
                    
                    WanderRadius = authoring.wanderRadius,
                    MinWanderTime = authoring.minWanderTime,
                    MaxWanderTime = authoring.maxWanderTime,
                    
                    Initialized = 0
                });
            }
        }
    }
}