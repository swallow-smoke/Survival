
using UnityEngine;

namespace _001_Scripts.Core._000_World._001_Water.Interface
{
    public interface IWaterQuery
    {
        bool IsInWater(Vector3 position);
        float GetSurfaceY(Vector3 position);
        IWaterbody GetWaterBody(Vector3 position);
    }
}