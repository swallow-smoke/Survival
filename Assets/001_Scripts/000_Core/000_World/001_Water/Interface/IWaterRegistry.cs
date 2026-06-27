namespace _001_Scripts.Core._000_World._001_Water.Interface
{
    public interface IWaterRegistry
    {
        void Register(IWaterbody waterbody);
        void UnRegister(IWaterbody waterbody);
    }
}