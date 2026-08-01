using System;
using UnityEngine;

namespace _001_Scripts.Controller
{
    /// <summary>Legacy no-op component retained so old prefabs do not lose a script.</summary>
    [Obsolete("BuoyancyController now samples IWaterQueryService directly.")]
    public sealed class BuoyancyTriggerProxy : MonoBehaviour
    {
        public void Initialize(_001_Scripts.Structure.BuoyancyController owner) { }
    }
}
