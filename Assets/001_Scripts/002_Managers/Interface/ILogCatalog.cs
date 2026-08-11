using System.Collections.Generic;
using _001_Scripts.Data;

namespace _001_Scripts.Interface
{
    public interface ILogCatalog
    {
        LogEntry Get(string id);
        IReadOnlyList<LogEntry> GetAll();
        bool Exists(string id);
    }
}
