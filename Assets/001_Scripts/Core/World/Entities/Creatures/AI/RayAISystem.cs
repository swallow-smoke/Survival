using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using WorldBuilder.Entities.Creatures;
using WorldBuilder.Entities.Creatures.Systems;

namespace AstraNope.Core.World.Entities.Creatures.AI
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(CreatureSwimSystem))]
    public partial struct RayAISystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new BankJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        [WithDisabled(typeof(CreatureCaptured))]
        [WithNone(typeof(CreatureRayLocomotion))]
        private partial struct BankJob : IJobEntity
        {
            public float DeltaTime;

            private void Execute(ref LocalTransform transform, in RayAI ray, in CreatureSwim swim,
                in WorldBuilder.Entities.WorldEntityActive active)
            {
                float3 forward = math.forward(transform.Rotation);
                float3 desired = math.normalizesafe(swim.TargetPoint - transform.Position, forward);
                float signedTurn = math.clamp(math.dot(math.cross(forward, desired), math.up()), -1f, 1f);
                float bank = -signedTurn * math.max(0f, ray.MaximumBankRadians);

                quaternion levelRotation = quaternion.LookRotationSafe(forward, math.up());
                quaternion bankRotation = quaternion.RotateZ(bank);
                quaternion targetRotation = math.mul(levelRotation, bankRotation);
                float blend = 1f - math.exp(-math.max(0f, ray.BankResponsiveness) * DeltaTime);
                transform.Rotation = math.slerp(transform.Rotation, targetRotation, math.saturate(blend));
            }
        }
    }
}
