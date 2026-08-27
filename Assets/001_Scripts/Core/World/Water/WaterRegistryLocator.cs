using System;
using AstraNope.Core.World.Water.Interfaces;

namespace AstraNope.Core.World.Water
{
    internal static class WaterRegistryLocator
    {
        public static IWaterRegistry Current { get; private set; }
        public static event Action<IWaterRegistry> RegistryAvailable;

        public static void Set(IWaterRegistry registry)
        {
            Current = registry;
            RegistryAvailable?.Invoke(registry);
        }

        public static void Clear(IWaterRegistry registry)
        {
            if (ReferenceEquals(Current, registry)) Current = null;
        }
    }
}
