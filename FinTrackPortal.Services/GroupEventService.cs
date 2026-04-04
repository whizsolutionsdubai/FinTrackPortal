using FinTrackPortal.Common;
using FinTrackPortal.Interfaces;
using FinTrackPortal.Models;

namespace FinTrackPortal.Services;

public sealed class GroupEventService : IGroupEventService
{
    private readonly IGroupEventRepository _repository;

    public GroupEventService(IGroupEventRepository repository)
    {
        _repository = repository;
    }

    public async Task<OperationResult<IReadOnlyList<GroupEventResponse>>> GetAsync(long groupId, long requestingMemberId)
    {
        var list = await _repository.GetForGroupAsync(groupId, requestingMemberId);
        return OperationResult<IReadOnlyList<GroupEventResponse>>.Success(list);
    }

    public async Task<OperationResult<long>> CreateAsync(long groupId, long createdByMemberId, CreateGroupEventRequest request)
    {
        var id = await _repository.CreateAsync(
            groupId,
            createdByMemberId,
            request.Title,
            request.Description,
            request.EventDate,
            request.Location);
        if (id == null || id.Value == 0)
            return OperationResult<long>.Failure("Could not create event. You may not be a member of this group.");
        return OperationResult<long>.Success(id.Value);
    }

    public async Task<OperationResult<bool>> UpdateAsync(long groupId, long eventId, long requestingMemberId, UpdateGroupEventRequest request)
    {
        var n = await _repository.UpdateAsync(groupId, eventId, requestingMemberId, request.Title, request.Description, request.EventDate, request.Location);
        if (n <= 0)
            return OperationResult<bool>.Failure("Event not found or you are not allowed to edit it.");
        return OperationResult<bool>.Success(true);
    }

    public async Task<OperationResult<bool>> DeleteAsync(long groupId, long eventId, long requestingMemberId)
    {
        var n = await _repository.DeleteAsync(groupId, eventId, requestingMemberId);
        if (n <= 0)
            return OperationResult<bool>.Failure("Event not found or you are not allowed to delete it.");
        return OperationResult<bool>.Success(true);
    }
}
