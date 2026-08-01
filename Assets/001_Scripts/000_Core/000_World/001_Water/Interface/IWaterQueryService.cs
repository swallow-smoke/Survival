using UnityEngine;

namespace _001_Scripts.Core._000_World._001_Water.Interface
{
    public interface IWaterQueryService
    {
        bool TrySample(Vector3 worldPosition, out WaterSample sample);
        bool TryGetWaterBody(Vector3 worldPosition, out IWaterBody waterBody);
    }
}
