using UnityEngine;

namespace _001_Scripts.Core._000_World._001_Water.Interface
{
    public interface IWaterbody
    {
        float GetSurfaceY(Vector3 position);
        
        bool Contain(Vector3 position);
    }
}