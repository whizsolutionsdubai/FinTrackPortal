using FinTrackPortal.Common;
using FinTrackPortal.Interfaces;
using FinTrackPortal.Models;

namespace FinTrackPortal.Services
{
    /// <summary>Delegates all group operations to <see cref="IGroupRepository"/>.</summary>
    public class GroupService : IGroupService
    {
        private readonly IGroupRepository _repository;

        public GroupService(IGroupRepository repository)
        {
            _repository = repository;
        }

        public Task<OperationResult<long>> CreateGroupAsync(string groupName, long createdByMemberId, string createdBy)
            => _repository.CreateGroupAsync(groupName, createdByMemberId, createdBy);

        public Task<OperationResult<bool>> AddMemberToGroupAsync(long groupId, long memberId, string role, string createdBy)
            => _repository.AddMemberToGroupAsync(groupId, memberId, role, createdBy);

        public Task<OperationResult<GroupSummaryResponse>> GetGroupSummaryAsync(long groupId)
            => _repository.GetGroupSummaryAsync(groupId);

        public Task<OperationResult<List<MyGroupResponse>>> GetMyGroupsAsync(long memberId)
            => _repository.GetMyGroupsAsync(memberId);

        public Task<OperationResult<List<GroupMemberResponse>>> GetGroupMembersAsync(long groupId)
            => _repository.GetGroupMembersAsync(groupId);

        public Task<OperationResult<bool>> IsMemberOfGroupAsync(long groupId, long memberId)
            => _repository.IsMemberOfGroupAsync(groupId, memberId);
    }
}
