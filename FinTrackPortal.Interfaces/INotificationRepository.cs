using FinTrackPortal.Models;

namespace FinTrackPortal.Interfaces;

public interface INotificationRepository
{
    Task<IReadOnlyList<NotificationItem>> GetForMemberAsync(long memberId, bool unreadOnly, int take);

    Task<int> MarkReadAsync(long notificationId, long memberId);

    Task<long> CreateAsync(long memberId, string title, string? body, string? notificationType, string? linkUrl);
}
