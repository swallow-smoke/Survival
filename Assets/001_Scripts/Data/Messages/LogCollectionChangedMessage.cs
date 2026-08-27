namespace AstraNope.Data.Messages
{
    public readonly struct LogCollectionChangedMessage
    {
        public readonly string LogId;
        public LogCollectionChangedMessage(string logId) => LogId = logId ?? string.Empty;
    }
}
