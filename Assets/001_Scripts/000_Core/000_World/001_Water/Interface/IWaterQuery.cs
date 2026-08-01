using UnityEngine;

namespace _001_Scripts.Core._000_World._001_Water.Interface
{
    /// <summary>Compatibility surface for the original water API.</summary>
    [System.Obsolete("Use IWaterQueryService.TrySample instead.")]
    public interface IWaterQuery : IWaterQueryService
    {
        bool IsInWater(Vector3 position);
        float GetSurfaceY(Vector3 position);
        IWaterbody GetWaterBody(Vector3 position);
    }
}
