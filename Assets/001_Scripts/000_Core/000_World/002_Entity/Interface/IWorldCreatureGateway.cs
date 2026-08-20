using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using WorldBuilder.Entities.Creatures;

namespace _001_Scripts._000_Core._000_World._002_Entity.Interface
{
    public interface IWorldCreatureGateway
    {
        bool IsReady { get; }
        bool TryRaycast(Vector3 origin, Vector3 direction, float distance, out Entity target, out float fraction);
        bool TryGetInteractionInfo(Entity target, out CreatureInteractionInfo info);
        bool TryCapture(Entity target, int toolItemId, byte toolTier);
        bool TryFeed(Entity target, int itemId, Vector3 sourcePosition);
        bool TryRecolor(Entity target, CreatureColorSlot slot, int paletteId);
        bool TrySetPattern(Entity target, CreaturePatternKind pattern, int paletteId, float strength);
        bool TryGetPaletteColor(int paletteId, out Color color);
        int DrainCaptureResults(Action<CreatureCaptureResult> visitor);
        int DrainFeedResults(Action<CreatureFeedResult> visitor);
        int DrainRecolorResults(Action<CreatureRecolorResult> visitor);

        bool TryDespawn(Entity target);
        int DespawnAll(CreatureFilter filter);
        int Count(CreatureFilter filter);
        int Collect(List<CreatureRecord> destination, CreatureFilter filter);
        bool TryFindNearest(Vector3 position, float maximumDistance, CreatureFilter filter,
            out CreatureRecord record);
        bool TryGetRecord(Entity target, out CreatureRecord record);

        bool SetPlayerFocus(Vector3 position);
        bool ClearPlayerFocus();
        bool TrySettle(Entity target, Entity habitat);
        bool TryUnsettle(Entity target);
        bool TryPreviewSettle(Entity target, Entity habitat, out CreatureSettleFailure failure);
        int CollectHabitats(List<CreatureHabitatRecord> destination);
        bool TryFindNearestHabitat(Vector3 position, float maximumDistance, out CreatureHabitatRecord record);
        int CollectStorageContents(Entity storage, List<CreatureStorageSlot> destination);
        int DrainSettleResults(Action<CreatureSettleResult> visitor);
        int DrainWorkEvents(Action<CreatureWorkCompletedEvent> visitor);

        int CaptureSnapshot(List<CreatureSnapshot> destination);
        int RestoreSnapshot(IReadOnlyList<CreatureSnapshot> snapshots);
    }

    public interface ICreatureSpawner
    {
        bool Spawn(int prefabId, Vector3 position, Quaternion rotation);
        bool Spawn(int prefabId, Vector3 position, Quaternion rotation, CreatureGrade grade);
        bool Spawn(int prefabId, Vector3 position, Quaternion rotation, in CreatureAppearance appearance);
        bool Spawn(int prefabId, Vector3 position, Quaternion rotation, CreatureGrade grade,
            in CreatureAppearance appearance);
    }

    public interface IWorldEntityGateway
    {
        bool IsReady { get; }
        bool TrySpawn(int prefabId, Vector3 position, Quaternion rotation, float uniformScale = 1f);
    }
}
