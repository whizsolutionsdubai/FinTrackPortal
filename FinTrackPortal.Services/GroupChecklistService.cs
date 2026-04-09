using FinTrackPortal.Common;
using FinTrackPortal.Interfaces;
using FinTrackPortal.Models;

namespace FinTrackPortal.Services;

public sealed class GroupChecklistService : IGroupChecklistService
{
    private readonly IGroupChecklistRepository _repository;

    public GroupChecklistService(IGroupChecklistRepository repository)
    {
        _repository = repository;
    }

    public async Task<OperationResult<IReadOnlyList<EventChecklistItemResponse>>> GetChecklistAsync(long eventId, long requestingMemberId)
        => OperationResult<IReadOnlyList<EventChecklistItemResponse>>.Success(await _repository.GetChecklistAsync(eventId, requestingMemberId));

    public async Task<OperationResult<long>> AddItemAsync(long eventId, AddChecklistItemRequest request)
    {
        var id = await _repository.AddItemAsync(eventId, request);
        return id.HasValue && id.Value > 0
            ? OperationResult<long>.Success(id.Value)
            : OperationResult<long>.Failure("Could not add checklist item.");
    }

    public async Task<OperationResult<bool>> UpdateItemAsync(long checklistItemId, UpdateChecklistItemRequest request)
    {
        var (result, message) = await _repository.UpdateItemAsync(checklistItemId, request);
        return result > 0 ? OperationResult<bool>.Success(true) : OperationResult<bool>.Failure(message ?? "Update failed.");
    }

    public async Task<OperationResult<bool>> DeleteItemAsync(long checklistItemId)
    {
        var rows = await _repository.DeleteItemAsync(checklistItemId);
        return rows > 0 ? OperationResult<bool>.Success(true) : OperationResult<bool>.Failure("Checklist item not found.");
    }

    public async Task<OperationResult<bool>> ClaimItemAsync(long checklistItemId, long memberId, int quantityClaimed)
    {
        var (result, message) = await _repository.ClaimItemAsync(checklistItemId, memberId, quantityClaimed);
        return result > 0 ? OperationResult<bool>.Success(true) : OperationResult<bool>.Failure(message ?? "Claim failed.");
    }

    public async Task<OperationResult<bool>> WithdrawClaimAsync(long checklistItemId, long memberId)
    {
        var rows = await _repository.WithdrawClaimAsync(checklistItemId, memberId);
        return rows > 0 ? OperationResult<bool>.Success(true) : OperationResult<bool>.Failure("No active claim to withdraw.");
    }

    public async Task<OperationResult<bool>> MarkCompleteAsync(long groupId, long eventId, long checklistItemId, long requestingMemberId)
    {
        var (result, message) = await _repository.MarkCompleteAsync(groupId, eventId, checklistItemId, requestingMemberId);
        return result > 0 ? OperationResult<bool>.Success(true) : OperationResult<bool>.Failure(message ?? "Could not mark item as completed.");
    }
}
