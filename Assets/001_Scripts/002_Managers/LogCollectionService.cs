using System;
using System.Collections.Generic;
using _001_Scripts.Data;
using _001_Scripts.Data.Message;
using _001_Scripts.Interface;
using MessagePipe;

namespace _001_Scripts.Managers
{
    public sealed class LogCollectionService : ILogCollectionService
    {
        private readonly List<LogEntry> _logs = new();
        private readonly HashSet<string> _ids = new(StringComparer.OrdinalIgnoreCase);
        private readonly IPublisher<LogCollectionChangedMessage> _publisher;

        public LogCollectionService(IPublisher<LogCollectionChangedMessage> publisher) => _publisher = publisher;

        public bool Add(LogEntry entry)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.id) || !_ids.Add(entry.id)) return false;
            var stored = entry.Clone();
            if (string.IsNullOrWhiteSpace(stored.title)) stored.title = stored.id;
            _logs.Add(stored);
            _publisher.Publish(new LogCollectionChangedMessage(stored.id));
            return true;
        }

        public IReadOnlyList<LogEntry> GetAllLogs() => _logs;
        public bool Contains(string id) => !string.IsNullOrWhiteSpace(id) && _ids.Contains(id);
    }
}
