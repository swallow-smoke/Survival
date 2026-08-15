using System;
using Unity.Entities;
using UnityEngine;
using WorldBuilder.Entities.Creatures;

namespace _001_Scripts.Interface
{
    public interface IWorldCreatureGateway
    {
        bool TryRaycast(Vector3 origin, Vector3 direction, float distance, out Entity target, out float fraction);
        bool TryGetInteractionInfo(Entity target, out CreatureInteractionInfo info);
        bool TryCapture(Entity target, int toolItemId, byte toolTier);
        bool TryFeed(Entity target, int itemId);
        int DrainCaptureResults(Action<CreatureCaptureResult> visitor);
        int DrainFeedResults(Action<CreatureFeedResult> visitor);
    }

    public interface ICreatureSpawner
    {
        bool Spawn(int prefabId, Vector3 position, Quaternion rotation);
        bool Spawn(int prefabId, Vector3 position, Quaternion rotation, CreatureGrade grade);
        bool Spawn(int prefabId, Vector3 position, Quaternion rotation, Color color);
        bool Spawn(int prefabId, Vector3 position, Quaternion rotation, CreatureGrade grade, Color color);
    }
}
