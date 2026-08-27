using UnityEngine;

namespace AstraNope.Core.World.Water.Interfaces
{
    public interface IWaterBody
    {
        int Priority { get; }
        Bounds WorldBounds { get; }
        bool TrySample(Vector3 worldPosition, out WaterSample sample);
    }
}
