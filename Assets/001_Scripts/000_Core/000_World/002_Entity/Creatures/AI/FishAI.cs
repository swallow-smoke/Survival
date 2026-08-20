using Unity.Entities;
using Unity.Mathematics;

namespace _001_Scripts._000_Core._000_World._002_Entity.Creatures.AI
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