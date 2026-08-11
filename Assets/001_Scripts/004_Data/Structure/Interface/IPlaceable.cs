namespace _001_Scripts.Data.Structure.Interface
{
    public interface IPlaceable
    {
        bool IsPlaced { get; }
        bool CanRotate { get; }
        void Place();
        void Remove();
    }
}
