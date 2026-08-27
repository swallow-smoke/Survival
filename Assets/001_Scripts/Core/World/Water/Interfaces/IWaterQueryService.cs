using UnityEngine;

namespace AstraNope.Core.World.Water.Interfaces
{
    public interface IWaterQueryService
    {
        bool TrySample(Vector3 worldPosition, out WaterSample sample);
        bool TryGetWaterBody(Vector3 worldPosition, out IWaterBody waterBody);
    }
}
