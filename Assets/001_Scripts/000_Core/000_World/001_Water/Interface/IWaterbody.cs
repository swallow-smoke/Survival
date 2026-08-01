using UnityEngine;

namespace _001_Scripts.Core._000_World._001_Water.Interface
{
    public interface IWaterBody
    {
        int Priority { get; }
        Bounds WorldBounds { get; }
        bool TrySample(Vector3 worldPosition, out WaterSample sample);
    }

    /// <summary>Compatibility contract for pre-integration water consumers.</summary>
    [System.Obsolete("Use IWaterBody.TrySample instead.")]
    public interface IWaterbody : IWaterBody
    {
        float GetSurfaceY(Vector3 position);
        bool Contain(Vector3 position);
    }
}
