using FinTrackPortal.Common;
using FinTrackPortal.Interfaces;
using FinTrackPortal.Models;

namespace FinTrackPortal.Services
{
    public class GroupService : IGroupService
    {
        private readonly IGroupRepository _repository;

        public GroupService(IGroupRepository repository)
        {
            _repository = repository;
        }

        public Task<OperationResult<long>> CreateGroupAsync(string groupName, long createdByMemberId, string createdBy)
            => _repository.CreateGroupAsync(groupName, createdByMemberId, createdBy);

        public Task<OperationResult<bool>> AddMemberToGroupAsync(long groupId, long memberId, string createdBy)
            => _repository.AddMemberToGroupAsync(groupId, memberId, createdBy);

        public Task<OperationResult<GroupSummaryResponse>> GetGroupSummaryAsync(long groupId)
            => _repository.GetGroupSummaryAsync(groupId);
    }
}
