using FinTrackPortal.Models;

namespace FinTrackPortal.Interfaces;

public interface IGroupEventRepository
{
    Task<IReadOnlyList<GroupEventResponse>> GetForGroupAsync(long groupId, long requestingMemberId);

    Task<long?> CreateAsync(long groupId, long createdByMemberId, string title, string? description, DateTime eventDate, string? location);

    Task<int> UpdateAsync(long groupId, long eventId, long requestingMemberId, string title, string? description, DateTime eventDate, string? location);

    Task<int> DeleteAsync(long groupId, long eventId, long requestingMemberId);
}
