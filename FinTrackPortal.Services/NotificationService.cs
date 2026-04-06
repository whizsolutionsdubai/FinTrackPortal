using FinTrackPortal.Interfaces;
using FinTrackPortal.Models;

namespace FinTrackPortal.Services;

public sealed class NotificationService : INotificationService
{
    private readonly INotificationRepository _repository;

    public NotificationService(INotificationRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<NotificationItem>> GetAsync(long memberId, bool unreadOnly, int take)
        => _repository.GetForMemberAsync(memberId, unreadOnly, take);

    public async Task<bool> MarkReadAsync(long notificationId, long memberId)
    {
        var n = await _repository.MarkReadAsync(notificationId, memberId);
        return n > 0;
    }

    public Task MarkAllReadAsync(long memberId)
        => _repository.MarkAllReadAsync(memberId);

    public Task<long> CreateAsync(long memberId, string title, string? body, string? notificationType, string? linkUrl)
        => _repository.CreateAsync(memberId, title, body, notificationType, linkUrl);
}
