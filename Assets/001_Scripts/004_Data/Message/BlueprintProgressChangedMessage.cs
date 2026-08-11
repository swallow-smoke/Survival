namespace _001_Scripts.Data.Message
{
    public readonly struct BlueprintProgressChangedMessage
    {
        public readonly int BlueprintId;
        public BlueprintProgressChangedMessage(int blueprintId) => BlueprintId = blueprintId;
    }
}
