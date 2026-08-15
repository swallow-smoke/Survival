using System;
using _001_Scripts.Interface;
using Unity.Entities;
using UnityEngine;
using WorldBuilder.Entities.Creatures;

namespace _001_Scripts.Managers
{
    public sealed class DotsWorldCreatureGateway : IWorldCreatureGateway, ICreatureSpawner
    {
        public bool TryRaycast(Vector3 origin, Vector3 direction, float distance, out Entity target,
            out float fraction)
        {
            return WorldCreatureCommandQueue.TryRaycast(origin, direction, distance, out target, out fraction);
        }

        public bool TryGetInteractionInfo(Entity target, out CreatureInteractionInfo info)
        {
            return WorldCreatureCommandQueue.TryGetInteractionInfo(target, out info);
        }

        public bool TryCapture(Entity target, int toolItemId, byte toolTier)
        {
            return WorldCreatureCommandQueue.TryCapture(target, toolItemId, toolTier, out _);
        }

        public bool TryFeed(Entity target, int itemId)
        {
            return WorldCreatureCommandQueue.TryFeed(target, itemId, out _);
        }

        public int DrainCaptureResults(Action<CreatureCaptureResult> visitor)
        {
            return WorldCreatureCommandQueue.DrainCaptureResults(visitor);
        }

        public int DrainFeedResults(Action<CreatureFeedResult> visitor)
        {
            return WorldCreatureCommandQueue.DrainFeedResults(visitor);
        }

        public bool Spawn(int prefabId, Vector3 position, Quaternion rotation)
        {
            return WorldCreatureCommandQueue.TrySpawn(prefabId, position, rotation);
        }

        public bool Spawn(int prefabId, Vector3 position, Quaternion rotation, CreatureGrade grade)
        {
            return WorldCreatureCommandQueue.TrySpawn(prefabId, position, rotation, grade);
        }

        public bool Spawn(int prefabId, Vector3 position, Quaternion rotation, Color color)
        {
            return WorldCreatureCommandQueue.TrySpawn(prefabId, position, rotation, color);
        }

        public bool Spawn(int prefabId, Vector3 position, Quaternion rotation, CreatureGrade grade, Color color)
        {
            return WorldCreatureCommandQueue.TrySpawn(prefabId, position, rotation, grade, color);
        }
    }
}
