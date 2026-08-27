using System;
using System.Collections.Generic;
using AstraNope.Core.World.Entities.Interfaces;
using AstraNope.Contracts;
using Unity.Mathematics;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using WorldBuilder.Entities.Creatures;

namespace AstraNope.Core.World.Entities
{
    public sealed class EntityManager : IEntityManager, ITickable
    {
        private readonly IWorldCreatureGateway creatures;
        private readonly ICreatureSpawner creatureSpawner;
        private readonly IWorldEntityGateway worldEntities;
        private readonly List<CreatureRecord> recordBuffer = new List<CreatureRecord>(64);
        private readonly List<CreatureHabitatRecord> habitatBuffer = new List<CreatureHabitatRecord>(16);
        private readonly Action<CreatureCaptureResult> onCaptureResult;
        private readonly Action<CreatureFeedResult> onFeedResult;
        private readonly Action<CreatureRecolorResult> onRecolorResult;
        private readonly Action<CreatureSettleResult> onSettleResult;
        private readonly Action<CreatureWorkCompletedEvent> onWorkEvent;

        public event Action<CreatureCaptureEvent> CaptureCompleted;
        public event Action<CreatureFeedEvent> FeedCompleted;
        public event Action<CreatureRecolorEvent> RecolorCompleted;
        public event Action<CreatureSettleEvent> SettlementChanged;
        public event Action<CreatureWorkEvent> WorkCompleted;

        [Inject]
        public EntityManager(IWorldCreatureGateway creatures, ICreatureSpawner creatureSpawner,
            IWorldEntityGateway worldEntities)
        {
            this.creatures = creatures;
            this.creatureSpawner = creatureSpawner;
            this.worldEntities = worldEntities;
            onCaptureResult = PublishCapture;
            onFeedResult = PublishFeed;
            onRecolorResult = PublishRecolor;
            onSettleResult = PublishSettle;
            onWorkEvent = PublishWork;
        }

        public bool IsReady => creatures.IsReady;

        public void Tick()
        {
            if (!IsReady) return;
            creatures.DrainCaptureResults(onCaptureResult);
            creatures.DrainFeedResults(onFeedResult);
            creatures.DrainRecolorResults(onRecolorResult);
            creatures.DrainSettleResults(onSettleResult);
            creatures.DrainWorkEvents(onWorkEvent);
        }

        public bool SpawnCreature(int prefabId, Vector3 position, Quaternion rotation)
            => creatureSpawner.Spawn(prefabId, position, rotation);

        public bool SpawnCreature(int prefabId, Vector3 position, Quaternion rotation, CreatureGrade grade)
            => creatureSpawner.Spawn(prefabId, position, rotation, grade);

        public bool SpawnWorldEntity(int prefabId, Vector3 position, Quaternion rotation, float uniformScale = 1f)
            => worldEntities.TrySpawn(prefabId, position, rotation, uniformScale);

        public int CreatureCount(CreatureFilter filter) => creatures.Count(filter);

        public int CollectCreatures(List<CreatureView> destination, CreatureFilter filter)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            destination.Clear();
            creatures.Collect(recordBuffer, filter);
            for (int i = 0; i < recordBuffer.Count; i++) destination.Add(ToView(recordBuffer[i]));
            return destination.Count;
        }

        public bool TryGetCreature(EntityHandle handle, out CreatureView view)
        {
            view = default;
            if (!handle.IsValid || !creatures.TryGetRecord(handle.Value, out CreatureRecord record)) return false;
            view = ToView(record);
            return true;
        }

        public bool TryFindNearestCreature(Vector3 position, float maximumDistance, CreatureFilter filter,
            out CreatureView view)
        {
            view = default;
            if (!creatures.TryFindNearest(position, maximumDistance, filter, out CreatureRecord record))
                return false;
            view = ToView(record);
            return true;
        }

        public bool TryRaycastCreature(Vector3 origin, Vector3 direction, float distance, out EntityHandle handle)
        {
            bool hit = creatures.TryRaycast(origin, direction, distance, out var target, out _);
            handle = hit ? new EntityHandle(target) : EntityHandle.None;
            return hit;
        }

        public bool TryGetPaletteColor(int paletteId, out Color color)
            => creatures.TryGetPaletteColor(paletteId, out color);

        public bool Despawn(EntityHandle handle) => handle.IsValid && creatures.TryDespawn(handle.Value);

        public int DespawnAllCreatures(CreatureFilter filter) => creatures.DespawnAll(filter);

        public int CaptureSnapshot(List<CreatureSnapshot> destination)
            => creatures.CaptureSnapshot(destination);

        public int RestoreSnapshot(IReadOnlyList<CreatureSnapshot> snapshots)
            => creatures.RestoreSnapshot(snapshots);

        public bool Capture(EntityHandle handle, int toolItemId = -1, byte toolTier = 0)
            => handle.IsValid && creatures.TryCapture(handle.Value, toolItemId, toolTier);

        public bool Feed(EntityHandle handle, int itemId, Vector3 sourcePosition)
            => handle.IsValid && creatures.TryFeed(handle.Value, itemId, sourcePosition);

        public bool Recolor(EntityHandle handle, CreatureColorSlot slot, int paletteId)
            => handle.IsValid && creatures.TryRecolor(handle.Value, slot, paletteId);

        public bool SetPattern(EntityHandle handle, CreaturePatternKind pattern, int paletteId,
            float strength = 1f)
            => handle.IsValid && creatures.TrySetPattern(handle.Value, pattern, paletteId, strength);

        public bool Settle(EntityHandle creature, EntityHandle habitat)
            => creature.IsValid && habitat.IsValid && creatures.TrySettle(creature.Value, habitat.Value);

        public bool Unsettle(EntityHandle creature)
            => creature.IsValid && creatures.TryUnsettle(creature.Value);

        public bool CanSettle(EntityHandle creature, EntityHandle habitat, out CreatureSettleFailure failure)
        {
            failure = CreatureSettleFailure.InvalidTarget;
            return creature.IsValid && habitat.IsValid &&
                   creatures.TryPreviewSettle(creature.Value, habitat.Value, out failure);
        }

        public int CollectHabitats(List<HabitatView> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            destination.Clear();
            creatures.CollectHabitats(habitatBuffer);
            for (int i = 0; i < habitatBuffer.Count; i++)
                destination.Add(new HabitatView(new EntityHandle(habitatBuffer[i].Entity), habitatBuffer[i]));
            return destination.Count;
        }

        public bool TryFindNearestHabitat(Vector3 position, float maximumDistance, out HabitatView view)
        {
            view = default;
            if (!creatures.TryFindNearestHabitat(position, maximumDistance, out CreatureHabitatRecord record))
                return false;
            view = new HabitatView(new EntityHandle(record.Entity), record);
            return true;
        }

        public int CollectStorage(EntityHandle storage, List<CreatureStorageSlot> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            destination.Clear();
            return storage.IsValid ? creatures.CollectStorageContents(storage.Value, destination) : 0;
        }

        private void PublishSettle(CreatureSettleResult result)
        {
            SettlementChanged?.Invoke(new CreatureSettleEvent(new EntityHandle(result.Target),
                new EntityHandle(result.Habitat), result.Failure, result.Roles, result.WorkSpeed,
                result.CarryCapacity));
        }

        private void PublishWork(CreatureWorkCompletedEvent completed)
        {
            WorkCompleted?.Invoke(new CreatureWorkEvent(new EntityHandle(completed.Worker),
                new EntityHandle(completed.Storage), completed.Role, completed.ItemId, completed.Count,
                completed.Accepted));
        }

        private void PublishCapture(CreatureCaptureResult result)
        {
            CaptureCompleted?.Invoke(new CreatureCaptureEvent(new EntityHandle(result.Target), result.Failure,
                result.ItemId, result.Count));
        }

        private void PublishFeed(CreatureFeedResult result)
        {
            FeedCompleted?.Invoke(new CreatureFeedEvent(new EntityHandle(result.Target), result.Failure,
                result.Affinity, result.MaximumAffinity, result.SuccessChance, result.AttemptCount,
                result.TamedNow != 0, result.IsTamed != 0));
        }

        private void PublishRecolor(CreatureRecolorResult result)
        {
            RecolorCompleted?.Invoke(new CreatureRecolorEvent(new EntityHandle(result.Target), result.Failure,
                ToColor(result.Appearance.Primary), ToColor(result.Appearance.Secondary),
                ToColor(result.Appearance.Accent), ToColor(result.Appearance.PatternColor),
                result.Appearance.Pattern));
        }

        private static CreatureView ToView(in CreatureRecord record) => new CreatureView(
            new EntityHandle(record.Entity), record, ToColor(record.Appearance.Primary),
            ToColor(record.Appearance.Secondary), ToColor(record.Appearance.Accent),
            ToColor(record.Appearance.PatternColor));

        private static Color ToColor(float4 value) => new Color(value.x, value.y, value.z, value.w);
    }
}
