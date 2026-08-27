using AstraNope.Core.World.Entities.Interfaces;
using AstraNope.Contracts;
using Unity.Entities;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using EcsWorld = Unity.Entities.World;

namespace AstraNope.Core.World.Entities.Bridges
{
    public sealed class CreaturePlayerFocusBridge : ITickable
    {
        private readonly IPlayerTransformProvider player;
        private readonly IWorldCreatureGateway creatures;
        private bool wasValid;

        private EcsWorld _world;
        private Entity _focusEntity = Entity.Null;

        [Inject]
        public CreaturePlayerFocusBridge(IPlayerTransformProvider player, IWorldCreatureGateway creatures)
        {
            this.player = player;
            this.creatures = creatures;
        }

        public void Tick()
        {
            Transform target = player?.PlayerTrs;

            UpdateEntityFocus(target);

            if (!creatures.IsReady)
                return;

            if (target == null)
            {
                if (!wasValid)
                    return;

                creatures.ClearPlayerFocus();
                wasValid = false;

                return;
            }

            creatures.SetPlayerFocus(target.position);
            wasValid = true;
        }
        
        private void UpdateEntityFocus(Transform target)
        {
            EcsWorld world = EcsWorld.DefaultGameObjectInjectionWorld;

            if (world == null || !world.IsCreated)
                return;

            Unity.Entities.EntityManager entityManager = world.EntityManager;

            if (_world != world ||
                _focusEntity == Entity.Null ||
                !entityManager.Exists(_focusEntity))
            {
                _world = world;

                using EntityQuery query =
                    entityManager.CreateEntityQuery(
                        ComponentType.ReadOnly<EntityPlayerFocus>());

                _focusEntity = query.IsEmptyIgnoreFilter
                    ? entityManager.CreateEntity(typeof(EntityPlayerFocus))
                    : query.GetSingletonEntity();
            }

            if (target == null)
            {
                entityManager.SetComponentData(
                    _focusEntity,
                    new EntityPlayerFocus
                    {
                        isValid = 0
                    });

                return;
            }

            entityManager.SetComponentData(
                _focusEntity,
                new EntityPlayerFocus
                {
                    Position = target.position,
                    isValid = 1
                });
        }
    }
}
