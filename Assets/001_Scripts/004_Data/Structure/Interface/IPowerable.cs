namespace _001_Scripts.Data.Structure.Interface
{
    public interface IPowerState
    {
        bool IsPowered { get; }
    }

    public interface IPowerable : IPowerState
    {
        void PowerUp();
        void PowerDown();
    }
}
