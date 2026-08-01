namespace _001_Scripts.Core._000_World._001_Water.Interface
{
    public interface IWaterRegistry
    {
        bool Register(IWaterBody waterBody);
        bool Unregister(IWaterBody waterBody);
        void Refresh(IWaterBody waterBody);

        [System.Obsolete("Use Register(IWaterBody).")]
        void Register(IWaterbody waterbody);

        [System.Obsolete("Use Unregister(IWaterBody).")]
        void UnRegister(IWaterbody waterbody);
    }
}
