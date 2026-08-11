namespace _001_Scripts.Data.Message
{
    public readonly struct LogCollectionChangedMessage
    {
        public readonly string LogId;
        public LogCollectionChangedMessage(string logId) => LogId = logId ?? string.Empty;
    }
}
