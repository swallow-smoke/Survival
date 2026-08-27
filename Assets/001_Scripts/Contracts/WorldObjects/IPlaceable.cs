namespace AstraNope.Contracts.WorldObjects
{
    public interface IPlaceable
    {
        bool IsPlaced { get; }
        bool CanRotate { get; }
        void Place();
        void Remove();
    }
}
