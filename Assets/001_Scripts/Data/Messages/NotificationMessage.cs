namespace AstraNope.Data.Messages
{
    public enum NotificationKind
    {
        Info,
        ItemAdded,
        ItemRemoved,
        Warning
    }

    public readonly struct NotificationMessage
    {
        public readonly string Title;
        public readonly string Body;
        public readonly string Icon;
        public readonly NotificationKind Kind;
        public readonly float Duration;

        public NotificationMessage(string title, string body = null, string icon = null,
            NotificationKind kind = NotificationKind.Info, float duration = 3f)
        {
            Title = title ?? string.Empty;
            Body = body ?? string.Empty;
            Icon = icon ?? string.Empty;
            Kind = kind;
            Duration = duration > 0f ? duration : 3f;
        }
    }
}
