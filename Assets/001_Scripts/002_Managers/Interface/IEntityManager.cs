using System;
using System.Collections.Generic;
using UnityEngine;
using WorldBuilder.Entities;
using WorldBuilder.Entities.Creatures;

namespace _001_Scripts.Interface
{
    public readonly struct EntityHandle : IEquatable<EntityHandle>
    {
        internal readonly Unity.Entities.Entity Value;

        internal EntityHandle(Unity.Entities.Entity value) => Value = value;

        public static EntityHandle None => default;
        public bool IsValid => Value != Unity.Entities.Entity.Null;

        public bool Equals(EntityHandle other) => Value.Equals(other.Value);
        public override bool Equals(object obj) => obj is EntityHandle other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString();

        public static bool operator ==(EntityHandle left, EntityHandle right) => left.Equals(right);
        public static bool operator !=(EntityHandle left, EntityHandle right) => !left.Equals(right);
    }

    public readonly struct CreatureView
    {
        public readonly EntityHandle Handle;
        public readonly string DisplayName;
        public readonly CreatureGrade Grade;
        public readonly CreatureSizeClass SizeClass;
        public readonly CreaturePersonality Personality;
        public readonly Color Primary;
        public readonly Color Secondary;
        public readonly Color Accent;
        public readonly Color PatternColor;
        public readonly CreaturePatternKind Pattern;
        public readonly Vector3 Position;
        public readonly int PrefabId;
        public readonly Vector2Int Region;
        public readonly float Affinity;
        public readonly float MaximumAffinity;
        public readonly int TameAttempts;
        public readonly bool IsTamed;
        public readonly bool IsActive;

        public CreatureView(EntityHandle handle, in CreatureRecord record, Color primary, Color secondary,
            Color accent, Color patternColor)
        {
            Handle = handle;
            DisplayName = record.DisplayName;
            Grade = record.Grade;
            SizeClass = record.SizeClass;
            Personality = record.Personality;
            Primary = primary;
            Secondary = secondary;
            Accent = accent;
            PatternColor = patternColor;
            Pattern = record.Appearance.Pattern;
            Position = record.Position;
            PrefabId = record.PrefabId;
            Region = new Vector2Int(record.Region.x, record.Region.y);
            Affinity = record.Affinity;
            MaximumAffinity = record.MaximumAffinity;
            TameAttempts = record.TameAttempts;
            IsTamed = record.IsTamed;
            IsActive = record.IsActive;
        }

        public float AffinityRatio => MaximumAffinity <= 0f ? 0f : Mathf.Clamp01(Affinity / MaximumAffinity);
    }

    public readonly struct CreatureCaptureEvent
    {
        public readonly EntityHandle Handle;
        public readonly CreatureCaptureFailure Failure;
        public readonly int ItemId;
        public readonly int Count;

        public CreatureCaptureEvent(EntityHandle handle, CreatureCaptureFailure failure, int itemId, int count)
        {
            Handle = handle;
            Failure = failure;
            ItemId = itemId;
            Count = count;
        }

        public bool Succeeded => Failure == CreatureCaptureFailure.None;
    }

    public readonly struct CreatureFeedEvent
    {
        public readonly EntityHandle Handle;
        public readonly CreatureFeedFailure Failure;
        public readonly float Affinity;
        public readonly float MaximumAffinity;
        public readonly float SuccessChance;
        public readonly int AttemptCount;
        public readonly bool TamedNow;
        public readonly bool IsTamed;

        public CreatureFeedEvent(EntityHandle handle, CreatureFeedFailure failure, float affinity,
            float maximumAffinity, float successChance, int attemptCount, bool tamedNow, bool isTamed)
        {
            Handle = handle;
            Failure = failure;
            Affinity = affinity;
            MaximumAffinity = maximumAffinity;
            SuccessChance = successChance;
            AttemptCount = attemptCount;
            TamedNow = tamedNow;
            IsTamed = isTamed;
        }

        public bool Accepted => Failure == CreatureFeedFailure.None;
        public bool Failed => Accepted && !TamedNow;
        public float AffinityRatio => MaximumAffinity <= 0f ? 0f : Mathf.Clamp01(Affinity / MaximumAffinity);
    }

    public readonly struct CreatureRecolorEvent
    {
        public readonly EntityHandle Handle;
        public readonly CreatureRecolorFailure Failure;
        public readonly Color Primary;
        public readonly Color Secondary;
        public readonly Color Accent;
        public readonly Color PatternColor;
        public readonly CreaturePatternKind Pattern;

        public CreatureRecolorEvent(EntityHandle handle, CreatureRecolorFailure failure, Color primary,
            Color secondary, Color accent, Color patternColor, CreaturePatternKind pattern)
        {
            Handle = handle;
            Failure = failure;
            Primary = primary;
            Secondary = secondary;
            Accent = accent;
            PatternColor = patternColor;
            Pattern = pattern;
        }

        public bool Succeeded => Failure == CreatureRecolorFailure.None;
    }

    public readonly struct HabitatView
    {
        public readonly EntityHandle Handle;
        public readonly string DisplayName;
        public readonly Vector3 Position;
        public readonly CreatureEnvironmentMask Provided;
        public readonly int Capacity;
        public readonly int MemberCount;

        public HabitatView(EntityHandle handle, in CreatureHabitatRecord record)
        {
            Handle = handle;
            DisplayName = record.DisplayName;
            Position = record.Position;
            Provided = record.Provided;
            Capacity = record.Capacity;
            MemberCount = record.MemberCount;
        }

        public bool HasRoom => Capacity <= 0 || MemberCount < Capacity;
    }

    public readonly struct CreatureSettleEvent
    {
        public readonly EntityHandle Handle;
        public readonly EntityHandle Habitat;
        public readonly CreatureSettleFailure Failure;
        public readonly CreatureRole Roles;
        public readonly float WorkSpeed;
        public readonly int CarryCapacity;

        public CreatureSettleEvent(EntityHandle handle, EntityHandle habitat, CreatureSettleFailure failure,
            CreatureRole roles, float workSpeed, int carryCapacity)
        {
            Handle = handle;
            Habitat = habitat;
            Failure = failure;
            Roles = roles;
            WorkSpeed = workSpeed;
            CarryCapacity = carryCapacity;
        }

        public bool Succeeded => Failure == CreatureSettleFailure.None;
    }

    public readonly struct CreatureWorkEvent
    {
        public readonly EntityHandle Worker;
        public readonly EntityHandle Storage;
        public readonly CreatureRole Role;
        public readonly int ItemId;
        public readonly int Count;
        public readonly int Accepted;

        public CreatureWorkEvent(EntityHandle worker, EntityHandle storage, CreatureRole role, int itemId,
            int count, int accepted)
        {
            Worker = worker;
            Storage = storage;
            Role = role;
            ItemId = itemId;
            Count = count;
            Accepted = accepted;
        }

        public bool Delivered => Accepted > 0;
    }

    public interface IEntitySettlement
    {
        bool Settle(EntityHandle creature, EntityHandle habitat);
        bool Unsettle(EntityHandle creature);
        bool CanSettle(EntityHandle creature, EntityHandle habitat, out CreatureSettleFailure failure);
        int CollectHabitats(List<HabitatView> destination);
        bool TryFindNearestHabitat(Vector3 position, float maximumDistance, out HabitatView view);
        int CollectStorage(EntityHandle storage, List<CreatureStorageSlot> destination);
        event Action<CreatureSettleEvent> SettlementChanged;
        event Action<CreatureWorkEvent> WorkCompleted;
    }

    public interface IEntitySpawnService
    {
        bool IsReady { get; }
        bool SpawnCreature(int prefabId, Vector3 position, Quaternion rotation);
        bool SpawnCreature(int prefabId, Vector3 position, Quaternion rotation, CreatureGrade grade);
        bool SpawnWorldEntity(int prefabId, Vector3 position, Quaternion rotation, float uniformScale = 1f);
    }

    public interface IEntityDirectory
    {
        int CreatureCount(CreatureFilter filter);
        int CollectCreatures(List<CreatureView> destination, CreatureFilter filter);
        bool TryGetCreature(EntityHandle handle, out CreatureView view);
        bool TryFindNearestCreature(Vector3 position, float maximumDistance, CreatureFilter filter,
            out CreatureView view);
        bool TryRaycastCreature(Vector3 origin, Vector3 direction, float distance, out EntityHandle handle);
        bool TryGetPaletteColor(int paletteId, out Color color);
    }

    public interface IEntityLifecycle
    {
        bool Despawn(EntityHandle handle);
        int DespawnAllCreatures(CreatureFilter filter);
        int CaptureSnapshot(List<CreatureSnapshot> destination);
        int RestoreSnapshot(IReadOnlyList<CreatureSnapshot> snapshots);
    }

    public interface IEntityInteractions
    {
        bool Capture(EntityHandle handle, int toolItemId = -1, byte toolTier = 0);
        bool Feed(EntityHandle handle, int itemId, Vector3 sourcePosition);
        bool Recolor(EntityHandle handle, CreatureColorSlot slot, int paletteId);
        bool SetPattern(EntityHandle handle, CreaturePatternKind pattern, int paletteId, float strength = 1f);
        event Action<CreatureCaptureEvent> CaptureCompleted;
        event Action<CreatureFeedEvent> FeedCompleted;
        event Action<CreatureRecolorEvent> RecolorCompleted;
    }

    public interface IEntityManager : IEntitySpawnService, IEntityDirectory, IEntityLifecycle,
        IEntityInteractions, IEntitySettlement
    { }
}
