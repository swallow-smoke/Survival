using AstraNope.Data.Creatures;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using WorldBuilder.Entities.Creatures;

namespace AstraNope.Core.World.Entities.Creatures.AI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Survival/Creatures/Ray Spawn Zones")]
    public sealed class RaySpawnZonesAuthoring : MonoBehaviour
    {
        [SerializeField] private RaySpeciesCatalog catalog;

        private sealed class Baker : Baker<RaySpawnZonesAuthoring>
        {
            public override void Bake(RaySpawnZonesAuthoring authoring)
            {
                if (authoring.catalog == null) return;
                DependsOn(authoring.catalog);
                for (int i = 0; i < authoring.catalog.Species.Count; i++)
                {
                    RaySpeciesDefinition species = authoring.catalog.Species[i];
                    if (species == null || species.Model == null) continue;
                    Entity zone = CreateAdditionalEntity(TransformUsageFlags.None, false,
                        $"RaySpawnZone_{species.PrefabId}");
                    AddComponent(zone, LocalTransform.FromPositionRotation(
                        species.SpawnCenter, quaternion.identity));
                    AddComponent(zone, new CreatureSpawnZone
                    {
                        PrefabId = species.PrefabId,
                        HalfExtents = math.max((float3)species.SpawnVolume * 0.5f, new float3(0.05f)),
                        AllowedGrades = CreatureGradeMask.All,
                        SpawnInterval = math.max(0.01f, species.SpawnInterval),
                        MaximumAlive = math.max(0, species.MaximumAlive),
                        SpawnPerTick = math.max(1, species.SpawnPerTick),
                        RandomState = CreatureGradeRules.SanitizeSeed((uint)species.PrefabId)
                    });
                    AddBuffer<CreatureSpawnedEntity>(zone);
                }
            }
        }
    }
}
