using Unity.Entities;

namespace AstraNope.Core.World.Entities.Creatures.AI
{
    /// <summary>
    /// Adds ray-specific banking to the shared WorldBuilder creature swim pipeline.
    /// Navigation, fleeing, orders, spawning and streaming remain owned by CreatureSwim.
    /// </summary>
    public struct RayAI : IComponentData
    {
        public float MaximumBankRadians;
        public float BankResponsiveness;
    }
}
