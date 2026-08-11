using System.Collections.Generic;
using _001_Scripts.Data;

namespace _001_Scripts.Interface
{
    public interface ILogCollectionReader
    {
        IReadOnlyList<LogEntry> GetAllLogs();
        bool Contains(string id);
    }

    public interface ILogCollectionWriter
    {
        bool Add(LogEntry entry);
    }

    public interface ILogCollectionService : ILogCollectionReader, ILogCollectionWriter { }
}
