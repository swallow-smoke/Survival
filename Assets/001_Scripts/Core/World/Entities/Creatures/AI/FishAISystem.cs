using AstraNope.Core.World.Entities.Bridges;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace AstraNope.Core.World.Entities.Creatures.AI
{
    [BurstCompile]
    public partial struct FishAISystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton(
                    out EntityPlayerFocus playerFocus))
                return;

            if (playerFocus.isValid == 0)
                return;

            float deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (ai, transform, entity) in
                     SystemAPI
                         .Query<RefRW<FishAI>, RefRW<LocalTransform>>()
                         .WithEntityAccess())
            {
                ref FishAI fish = ref ai.ValueRW;
                ref LocalTransform localTransform = ref transform.ValueRW;

                if (fish.Initialized == 0)
                {
                    Initialize(
                        ref fish,
                        localTransform.Position,
                        entity);
                }

                float3 position = localTransform.Position;

                float3 fromPlayer =
                    position - playerFocus.Position;

                float distanceSquared =
                    math.lengthsq(fromPlayer);

                float fleeDistanceSquared =
                    fish.FleeDistance * fish.FleeDistance;

                float3 desiredDirection;
                float speed;

                if (distanceSquared < fleeDistanceSquared)
                {
                    // 플레이어 반대 방향
                    desiredDirection =
                        math.normalizesafe(fromPlayer);

                    speed = fish.FleeSpeed;

                    // 도망이 끝난 뒤 새로운 목표를 잡도록
                    fish.WanderTime = 0f;
                }
                else
                {
                    UpdateWander(
                        ref fish,
                        position,
                        deltaTime);

                    desiredDirection =
                        math.normalizesafe(
                            fish.WanderTarget - position);

                    speed = fish.MoveSpeed;
                }

                Move(
                    ref localTransform,
                    desiredDirection,
                    speed,
                    fish.TurnSpeed,
                    deltaTime);
            }
        }

        private static void Initialize(
            ref FishAI fish,
            float3 position,
            Entity entity)
        {
            fish.HomePosition = position;

            uint seed = math.hash(
                new uint2(
                    (uint)(entity.Index + 1),
                    (uint)(entity.Version + 1)));

            fish.RandomState =
                seed == 0 ? 1u : seed;

            fish.Initialized = 1;
            fish.WanderTime = 0f;
        }

        private static void UpdateWander(
            ref FishAI fish,
            float3 position,
            float deltaTime)
        {
            fish.WanderTime -= deltaTime;

            float3 toTarget =
                fish.WanderTarget - position;

            bool reachedTarget =
                math.lengthsq(toTarget) < 0.5f;

            if (fish.WanderTime > 0f &&
                !reachedTarget)
                return;

            var random =
                new Random(fish.RandomState);

            float3 direction =
                random.NextFloat3Direction();

            float distance =
                random.NextFloat(
                    fish.WanderRadius * 0.25f,
                    fish.WanderRadius);

            fish.WanderTarget =
                fish.HomePosition +
                direction * distance;

            fish.WanderTime =
                random.NextFloat(
                    fish.MinWanderTime,
                    fish.MaxWanderTime);

            fish.RandomState = random.state;
        }

        private static void Move(
            ref LocalTransform transform,
            float3 desiredDirection,
            float speed,
            float turnSpeed,
            float deltaTime)
        {
            if (math.lengthsq(desiredDirection) < 0.0001f)
                return;

            quaternion targetRotation =
                quaternion.LookRotationSafe(
                    desiredDirection,
                    math.up());

            float rotationT =
                math.saturate(turnSpeed * deltaTime);

            transform.Rotation =
                math.slerp(
                    transform.Rotation,
                    targetRotation,
                    rotationT);

            float3 forward =
                math.mul(
                    transform.Rotation,
                    new float3(0f, 0f, 1f));

            transform.Position +=
                forward *
                speed *
                deltaTime;
        }
    }
}