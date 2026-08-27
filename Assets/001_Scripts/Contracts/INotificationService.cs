using AstraNope.Data.Messages;

namespace AstraNope.Contracts
{
    public interface INotificationService
    {
        void Show(NotificationMessage message);
        void Show(string title, string body = null, string icon = null,
            NotificationKind kind = NotificationKind.Info, float duration = 3f);
    }
}
