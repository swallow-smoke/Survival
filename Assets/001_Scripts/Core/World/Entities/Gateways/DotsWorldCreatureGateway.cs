using System;
using System.Collections.Generic;
using AstraNope.Core.World.Entities.Interfaces;
using AstraNope.Contracts;
using Unity.Entities;
using UnityEngine;
using WorldBuilder.Entities;
using WorldBuilder.Entities.Creatures;

namespace AstraNope.Core.World.Entities.Gateways
{
    public sealed class DotsWorldCreatureGateway : IWorldCreatureGateway, ICreatureSpawner, IWorldEntityGateway
    {
        public bool IsReady => WorldEntityCommandQueue.IsReady;

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

        public bool TryFeed(Entity target, int itemId, Vector3 sourcePosition)
        {
            return WorldCreatureCommandQueue.TryFeed(target, itemId, sourcePosition, out _);
        }

        public bool TryRecolor(Entity target, CreatureColorSlot slot, int paletteId)
        {
            return WorldCreatureCommandQueue.TryRecolor(target, slot, paletteId, out _);
        }

        public bool TrySetPattern(Entity target, CreaturePatternKind pattern, int paletteId, float strength)
        {
            return WorldCreatureCommandQueue.TrySetPattern(target, pattern, paletteId, strength, out _);
        }

        public bool TryGetPaletteColor(int paletteId, out Color color)
        {
            return WorldCreatureCommandQueue.TryGetPaletteColor(paletteId, out color);
        }

        public int DrainCaptureResults(Action<CreatureCaptureResult> visitor)
        {
            return WorldCreatureCommandQueue.DrainCaptureResults(visitor);
        }

        public int DrainFeedResults(Action<CreatureFeedResult> visitor)
        {
            return WorldCreatureCommandQueue.DrainFeedResults(visitor);
        }

        public int DrainRecolorResults(Action<CreatureRecolorResult> visitor)
        {
            return WorldCreatureCommandQueue.DrainRecolorResults(visitor);
        }

        public bool TryDespawn(Entity target) => WorldCreatureCommandQueue.TryDespawn(target);

        public int DespawnAll(CreatureFilter filter) => WorldCreatureCommandQueue.DespawnAll(filter);

        public int Count(CreatureFilter filter) => WorldCreatureCommandQueue.Count(filter);

        public int Collect(List<CreatureRecord> destination, CreatureFilter filter)
            => WorldCreatureCommandQueue.Collect(destination, filter);

        public bool TryFindNearest(Vector3 position, float maximumDistance, CreatureFilter filter,
            out CreatureRecord record)
        {
            return WorldCreatureCommandQueue.TryFindNearest(position, maximumDistance, filter, out record);
        }

        public bool TryGetRecord(Entity target, out CreatureRecord record)
            => WorldCreatureCommandQueue.TryGetRecord(target, out record);

        public bool SetPlayerFocus(Vector3 position)
            => WorldCreatureCommandQueue.SetPlayerFocus(position);

        public bool ClearPlayerFocus() => WorldCreatureCommandQueue.ClearPlayerFocus();

        public bool TrySettle(Entity target, Entity habitat)
            => WorldCreatureCommandQueue.TrySettle(target, habitat, out _);

        public bool TryUnsettle(Entity target) => WorldCreatureCommandQueue.TryUnsettle(target, out _);

        public bool TryPreviewSettle(Entity target, Entity habitat, out CreatureSettleFailure failure)
            => WorldCreatureCommandQueue.TryPreviewSettle(target, habitat, out failure);

        public int CollectHabitats(List<CreatureHabitatRecord> destination)
            => WorldCreatureCommandQueue.CollectHabitats(destination);

        public bool TryFindNearestHabitat(Vector3 position, float maximumDistance,
            out CreatureHabitatRecord record)
            => WorldCreatureCommandQueue.TryFindNearestHabitat(position, maximumDistance, out record);

        public int CollectStorageContents(Entity storage, List<CreatureStorageSlot> destination)
            => WorldCreatureCommandQueue.CollectStorageContents(storage, destination);

        public int DrainSettleResults(Action<CreatureSettleResult> visitor)
            => WorldCreatureCommandQueue.DrainSettleResults(visitor);

        public int DrainWorkEvents(Action<CreatureWorkCompletedEvent> visitor)
            => WorldCreatureCommandQueue.DrainWorkEvents(visitor);

        public int CaptureSnapshot(List<CreatureSnapshot> destination)
            => CreatureSnapshotService.TryCapture(destination) ? destination.Count : 0;

        public int RestoreSnapshot(IReadOnlyList<CreatureSnapshot> snapshots)
            => CreatureSnapshotService.Restore(snapshots);

        public bool Spawn(int prefabId, Vector3 position, Quaternion rotation)
        {
            return WorldCreatureCommandQueue.TrySpawn(prefabId, position, rotation);
        }

        public bool Spawn(int prefabId, Vector3 position, Quaternion rotation, CreatureGrade grade)
        {
            return WorldCreatureCommandQueue.TrySpawn(prefabId, position, rotation, grade);
        }

        public bool Spawn(int prefabId, Vector3 position, Quaternion rotation,
            in CreatureAppearance appearance)
        {
            return WorldCreatureCommandQueue.TrySpawn(prefabId, position, rotation, appearance);
        }

        public bool Spawn(int prefabId, Vector3 position, Quaternion rotation, CreatureGrade grade,
            in CreatureAppearance appearance)
        {
            return WorldCreatureCommandQueue.TrySpawn(prefabId, position, rotation, grade, appearance);
        }

        public bool TrySpawn(int prefabId, Vector3 position, Quaternion rotation, float uniformScale = 1f)
        {
            return WorldEntityCommandQueue.TrySpawn(prefabId, position, rotation, uniformScale);
        }
    }
}
