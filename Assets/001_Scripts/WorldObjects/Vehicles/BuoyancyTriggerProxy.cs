using System;
using UnityEngine;

namespace AstraNope.WorldObjects.Vehicles
{
    /// <summary>Legacy no-op component retained so old prefabs do not lose a script.</summary>
    [Obsolete("BuoyancyController now samples IWaterQueryService directly.")]
    public sealed class BuoyancyTriggerProxy : MonoBehaviour
    {
        public void Initialize(BuoyancyController owner) { }
    }
}
