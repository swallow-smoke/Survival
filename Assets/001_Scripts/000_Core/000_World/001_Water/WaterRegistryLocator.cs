using System;
using _001_Scripts.Core._000_World._001_Water.Interface;

namespace _001_Scripts.Core._000_World._001_Water
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
