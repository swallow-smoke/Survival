namespace AstraNope.Contracts.WorldObjects
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
