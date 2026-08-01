using System;

namespace _001_Scripts.Core._000_World._001_Water
{
    /// <summary>Serialized compatibility component. Prefer LakeWaterBody for new content.</summary>
    [Obsolete("Use LakeWaterBody for new water volumes.")]
    public class LocalWaterVol : LakeWaterBody
    {
    }
}
