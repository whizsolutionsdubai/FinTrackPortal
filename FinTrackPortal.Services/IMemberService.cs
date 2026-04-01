using FinTrackPortal.Common;

namespace FinTrackPortal.Services
{
    public interface IMemberService
    {
        Task<OperationResult<long>> CreateMemberAsync(string memberName, string createdBy);
        Task<OperationResult<bool>> EditMemberAsync(long memberId, string memberName, string modifiedBy);
        Task<OperationResult<bool>> DeleteMemberAsync(long memberId, string modifiedBy);
    }
}
