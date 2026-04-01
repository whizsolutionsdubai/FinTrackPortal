using FinTrackPortal.Common;
using FinTrackPortal.Models;

namespace FinTrackPortal.Services
{
    /// <summary>
    /// Business-logic contract for group management.
    /// Currently a thin pass-through to <see cref="Interfaces.IGroupRepository"/>.
    /// </summary>
    public interface IGroupService
    {
        Task<OperationResult<long>> CreateGroupAsync(string groupName, long createdByMemberId, string createdBy);
        Task<OperationResult<bool>> AddMemberToGroupAsync(long groupId, long memberId, string role, string createdBy);
        Task<OperationResult<GroupSummaryResponse>> GetGroupSummaryAsync(long groupId);
        Task<OperationResult<List<MyGroupResponse>>> GetMyGroupsAsync(long memberId);
        Task<OperationResult<List<GroupMemberResponse>>> GetGroupMembersAsync(long groupId);
        Task<OperationResult<bool>> IsMemberOfGroupAsync(long groupId, long memberId);
    }
}
