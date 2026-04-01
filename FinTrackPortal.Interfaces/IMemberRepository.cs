using FinTrackPortal.Common;

namespace FinTrackPortal.Interfaces
{
    public interface IMemberRepository
    {
        Task<OperationResult<long>> CreateMemberAsync(string memberName, string createdBy);
        Task<OperationResult<bool>> EditMemberAsync(long memberId, string memberName, string modifiedBy);
        Task<OperationResult<bool>> DeleteMemberAsync(long memberId, string modifiedBy);
    }
}
