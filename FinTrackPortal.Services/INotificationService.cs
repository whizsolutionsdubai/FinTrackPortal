using FinTrackPortal.Models;

namespace FinTrackPortal.Services;

public interface INotificationService
{
    Task<IReadOnlyList<NotificationItem>> GetAsync(long memberId, bool unreadOnly, int take);

    Task<bool> MarkReadAsync(long notificationId, long memberId);
    Task MarkAllReadAsync(long memberId);

    Task<long> CreateAsync(long memberId, string title, string? body, string? notificationType, string? linkUrl);
}
