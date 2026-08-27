using UnityEngine;

namespace AstraNope.Core.World.Water.Interfaces
{
    /// <summary>Compatibility surface for the original water API.</summary>
    [System.Obsolete("Use IWaterQueryService.TrySample instead.")]
    public interface IWaterQuery : IWaterQueryService
    {
        bool IsInWater(Vector3 position);
        float GetSurfaceY(Vector3 position);
        IWaterBody GetWaterBody(Vector3 position);
    }
}
