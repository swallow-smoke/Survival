using System;
using UnityEngine;

namespace AstraNope.Data
{
    [Serializable]
    public sealed class LogEntry
    {
        public string id;
        public string title;
        [TextArea(3, 12)] public string body;
        [Tooltip("Optional Resources path without extension, e.g. Logs/AbandonedBase.")]
        public string imageResource;
        [NonSerialized] public Sprite image;

        public LogEntry Clone() => new()
        {
            id = id,
            title = title,
            body = body,
            imageResource = imageResource,
            image = image
        };
    }
}
