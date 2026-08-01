using System;
using _001_Scripts.Core._000_World._001_Water;

namespace _001_Scripts.Controller
{
    /// <summary>Legacy serialized alias. New scenes should use LakeWaterBody.</summary>
    [Obsolete("Use LakeWaterBody for water volumes.")]
    public class WaterVolume : LakeWaterBody
    {
    }
}
