using FinTrackPortal.Models;

namespace FinTrackPortal.Interfaces;

public interface IGroupChecklistRepository
{
    Task<IReadOnlyList<EventChecklistItemResponse>> GetChecklistAsync(long eventId, long requestingMemberId);
    Task<long?> AddItemAsync(long eventId, AddChecklistItemRequest request);
    Task<(int Result, string? Message)> UpdateItemAsync(long checklistItemId, UpdateChecklistItemRequest request);
    Task<int> DeleteItemAsync(long checklistItemId);
    Task<(int Result, string? Message)> ClaimItemAsync(long checklistItemId, long memberId, int quantityClaimed);
    Task<int> WithdrawClaimAsync(long checklistItemId, long memberId);
    Task<(int Result, string? Message)> MarkCompleteAsync(long groupId, long eventId, long checklistItemId, long requestingMemberId);
}
