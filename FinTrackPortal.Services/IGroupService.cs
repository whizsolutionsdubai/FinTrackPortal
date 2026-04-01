using FinTrackPortal.Common;
using FinTrackPortal.Models;

namespace FinTrackPortal.Services
{
    public interface IGroupService
    {
        Task<OperationResult<long>> CreateGroupAsync(string groupName, long createdByMemberId, string createdBy);
        Task<OperationResult<bool>> AddMemberToGroupAsync(long groupId, long memberId, string createdBy);
        Task<OperationResult<GroupSummaryResponse>> GetGroupSummaryAsync(long groupId);
    }
}
