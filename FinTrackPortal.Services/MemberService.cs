using FinTrackPortal.Common;
using FinTrackPortal.Interfaces;

namespace FinTrackPortal.Services
{
    /// <summary>Delegates all member operations to <see cref="IMemberRepository"/>.</summary>
    public class MemberService : IMemberService
    {
        private readonly IMemberRepository _repository;

        public MemberService(IMemberRepository repository)
        {
            _repository = repository;
        }

        public Task<OperationResult<long>> CreateMemberAsync(string memberName, string createdBy)
            => _repository.CreateMemberAsync(memberName, createdBy);

        public Task<OperationResult<bool>> EditMemberAsync(long memberId, string memberName, string modifiedBy)
            => _repository.EditMemberAsync(memberId, memberName, modifiedBy);

        public Task<OperationResult<bool>> DeleteMemberAsync(long memberId, string modifiedBy)
            => _repository.DeleteMemberAsync(memberId, modifiedBy);
    }
}
