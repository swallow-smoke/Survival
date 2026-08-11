using System;
using System.Collections.Generic;
using _001_Scripts.Data;
using _001_Scripts.Interface;
using UnityEngine;

namespace _001_Scripts.Managers
{
    public sealed class JsonLogCatalog : ILogCatalog
    {
        [Serializable]
        private sealed class LogCollection
        {
            public List<LogEntry> logs = new();
        }

        private readonly List<LogEntry> _logs = new();
        private readonly Dictionary<string, LogEntry> _byId =
            new(StringComparer.OrdinalIgnoreCase);

        public JsonLogCatalog() => Reload();

        public void Reload()
        {
            var source = Resources.Load<TextAsset>("Data/Logs");
            if (!source) throw new InvalidOperationException(
                "Log JSON was not found at Resources/Data/Logs.json.");
            LoadJson(source.text);
        }

        public void LoadJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("Log JSON cannot be empty.", nameof(json));
            var collection = JsonUtility.FromJson<LogCollection>(json);
            if (collection?.logs == null)
                throw new FormatException("Log JSON must contain a 'logs' array.");

            _logs.Clear();
            _byId.Clear();
            foreach (var entry in collection.logs)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.id))
                    throw new FormatException("Every log requires an id.");
                entry.id = entry.id.Trim();
                if (string.IsNullOrWhiteSpace(entry.title))
                    throw new FormatException($"Log '{entry.id}' requires a title.");
                if (!_byId.TryAdd(entry.id, entry))
                    throw new FormatException($"Duplicate log id: {entry.id}.");
                if (!string.IsNullOrWhiteSpace(entry.imageResource))
                    entry.image = Resources.Load<Sprite>(entry.imageResource.Trim());
                _logs.Add(entry);
            }
        }

        public LogEntry Get(string id) =>
            !string.IsNullOrWhiteSpace(id) && _byId.TryGetValue(id, out var entry) ? entry : null;
        public IReadOnlyList<LogEntry> GetAll() => _logs;
        public bool Exists(string id) => Get(id) != null;
    }
}
