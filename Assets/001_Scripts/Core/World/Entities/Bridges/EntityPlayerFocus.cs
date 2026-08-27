using Unity.Entities;
using Unity.Mathematics;

namespace AstraNope.Core.World.Entities.Bridges
{
    public struct EntityPlayerFocus : IComponentData
    {
        public float3 Position;
        public byte isValid;
    }
}