using FinTrackPortal.Common;
using FinTrackPortal.Models;

namespace FinTrackPortal.Interfaces
{
    /// <summary>
    /// Data-access contract for group management.
    /// Implemented by GroupRepository using Dapper + stored procedures.
    /// </summary>
    public interface IGroupRepository
    {
        /// <summary>Create a group and auto-add the creator as Admin (sp_CreateGroup — atomic SQL transaction).</summary>
        Task<OperationResult<long>> CreateGroupAsync(string groupName, long createdByMemberId, string createdBy);

        /// <summary>Add a member to an existing group (sp_AddMemberToGroup — duplicate check in SP).</summary>
        Task<OperationResult<bool>> AddMemberToGroupAsync(long groupId, long memberId, string role, string createdBy);

        /// <summary>Get per-member balance summary: paid, share, net (sp_GetGroupSummary).</summary>
        Task<OperationResult<GroupSummaryResponse>> GetGroupSummaryAsync(long groupId);

        /// <summary>List all groups for a given member (sp_GetMyGroups).</summary>
        Task<OperationResult<List<MyGroupResponse>>> GetMyGroupsAsync(long memberId);

        /// <summary>List all active members in a group with their roles (sp_GetGroupMembers).</summary>
        Task<OperationResult<List<GroupMemberResponse>>> GetGroupMembersAsync(long groupId);

        /// <summary>Check whether a member belongs to a group (sp_IsMemberOfGroup). Used for access control.</summary>
        Task<OperationResult<bool>> IsMemberOfGroupAsync(long groupId, long memberId);
    }
}
