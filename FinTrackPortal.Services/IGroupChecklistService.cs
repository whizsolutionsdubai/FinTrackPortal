using FinTrackPortal.Common;
using FinTrackPortal.Models;

namespace FinTrackPortal.Services;

public interface IGroupChecklistService
{
    Task<OperationResult<IReadOnlyList<EventChecklistItemResponse>>> GetChecklistAsync(long eventId, long requestingMemberId);
    Task<OperationResult<long>> AddItemAsync(long eventId, AddChecklistItemRequest request);
    Task<OperationResult<bool>> UpdateItemAsync(long checklistItemId, UpdateChecklistItemRequest request);
    Task<OperationResult<bool>> DeleteItemAsync(long checklistItemId);
    Task<OperationResult<bool>> ClaimItemAsync(long checklistItemId, long memberId, int quantityClaimed);
    Task<OperationResult<bool>> WithdrawClaimAsync(long checklistItemId, long memberId);
    Task<OperationResult<bool>> MarkCompleteAsync(long groupId, long eventId, long checklistItemId, long requestingMemberId);
}
