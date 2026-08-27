namespace AstraNope.Contracts
{
    public interface IBuildingPlacementService
    {
        bool IsPlacing { get; }
        int ActiveBlueprintId { get; }
        bool TryBegin(int blueprintId, out string failureReason);
        void Cancel();
    }

    public interface IBuildSelectionReader
    {
        int LastBlueprintId { get; }
    }
}
