using System.Collections.Generic;

namespace AstraNope.WorldObjects.Entities
{
    public interface IEntity
    {
        string EntityId { get; }
        string DisplayName { get; }
        EntityKind Kind { get; }

        bool TryGetFeature<T>(out T feature) where T : class;
        IReadOnlyList<T> GetFeatures<T>() where T : class;
    }

    public interface IDamageable
    {
        float CurrentHealth { get; }
        float MaxHealth { get; }
        float HealthRatio { get; }
        bool IsAlive { get; }
        void ApplyDamage(float amount);
    }

    public interface IRepairable
    {
        void RestoreHealth(float amount);
    }
}
