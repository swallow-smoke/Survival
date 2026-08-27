using Unity.Entities;
using Unity.Mathematics;

namespace AstraNope.Core.World.Entities.Creatures.AI
{
    public struct FishAI : IComponentData
    {
        public float MoveSpeed;
        public float FleeSpeed;
        public float FleeDistance;

        public float WanderRadius;
        public float TurnSpeed;
        
        public float MinWanderTime;
        public float MaxWanderTime;

        public float3 HomePosition;
        public float3 WanderTarget;

        public float WanderTime;

        public uint RandomState;
        public byte Initialized;
    }
}