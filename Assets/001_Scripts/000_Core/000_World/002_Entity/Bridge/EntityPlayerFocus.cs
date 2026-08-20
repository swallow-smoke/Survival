using Unity.Entities;
using Unity.Mathematics;

namespace _001_Scripts._000_Core._000_World._002_Entity.Bridge
{
    public struct EntityPlayerFocus : IComponentData
    {
        public float3 Position;
        public byte isValid;
    }
}