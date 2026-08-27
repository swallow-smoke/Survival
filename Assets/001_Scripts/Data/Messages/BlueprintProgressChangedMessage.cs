namespace AstraNope.Data.Messages
{
    public readonly struct BlueprintProgressChangedMessage
    {
        public readonly int BlueprintId;
        public BlueprintProgressChangedMessage(int blueprintId) => BlueprintId = blueprintId;
    }
}
