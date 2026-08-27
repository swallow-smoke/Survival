namespace AstraNope.Core.World.Water.Interfaces
{
    public interface IWaterRegistry
    {
        bool Register(IWaterBody waterBody);
        bool Unregister(IWaterBody waterBody);
        void Refresh(IWaterBody waterBody);
    }
}
