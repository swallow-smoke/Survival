using System.Collections.Generic;
using AstraNope.Data;

namespace AstraNope.Contracts
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
