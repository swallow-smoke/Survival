using System.Collections.Generic;
using AstraNope.Data;

namespace AstraNope.Contracts
{
    public interface ILogCatalog
    {
        LogEntry Get(string id);
        IReadOnlyList<LogEntry> GetAll();
        bool Exists(string id);
    }
}
