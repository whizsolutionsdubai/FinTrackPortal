using FinTrackPortal.Common;
using FinTrackPortal.Models;

namespace FinTrackPortal.Services;

public interface IGroupEventService
{
    Task<OperationResult<IReadOnlyList<GroupEventResponse>>> GetAsync(long groupId, long requestingMemberId);

    Task<OperationResult<long>> CreateAsync(long groupId, long createdByMemberId, CreateGroupEventRequest request);

    Task<OperationResult<bool>> UpdateAsync(long groupId, long eventId, long requestingMemberId, UpdateGroupEventRequest request);

    Task<OperationResult<bool>> DeleteAsync(long groupId, long eventId, long requestingMemberId);
}
