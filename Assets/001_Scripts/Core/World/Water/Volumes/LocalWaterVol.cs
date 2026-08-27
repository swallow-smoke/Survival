using System;

namespace AstraNope.Core.World.Water
{
    /// <summary>Serialized compatibility component. Prefer LakeWaterBody for new content.</summary>
    [Obsolete("Use LakeWaterBody for new water volumes.")]
    public class LocalWaterVol : LakeWaterBody
    {
    }
}
