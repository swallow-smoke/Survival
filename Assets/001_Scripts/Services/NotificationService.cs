using AstraNope.Data.Messages;
using AstraNope.Contracts;
using MessagePipe;

namespace AstraNope.Services
{
    public sealed class NotificationService : INotificationService
    {
        private readonly IPublisher<NotificationMessage> _publisher;

        public NotificationService(IPublisher<NotificationMessage> publisher) => _publisher = publisher;

        public void Show(NotificationMessage message) => _publisher.Publish(message);

        public void Show(string title, string body = null, string icon = null,
            NotificationKind kind = NotificationKind.Info, float duration = 3f) =>
            Show(new NotificationMessage(title, body, icon, kind, duration));
    }
}
