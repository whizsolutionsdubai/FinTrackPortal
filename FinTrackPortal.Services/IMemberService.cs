using FinTrackPortal.Common;

namespace FinTrackPortal.Services
{
    /// <summary>
    /// Business-logic contract for member profile CRUD.
    /// Currently a thin pass-through to <see cref="Interfaces.IMemberRepository"/>.
    /// </summary>
    public interface IMemberService
    {
        Task<OperationResult<long>> CreateMemberAsync(string memberName, string createdBy);
        Task<OperationResult<bool>> EditMemberAsync(long memberId, string memberName, string modifiedBy);
        Task<OperationResult<bool>> DeleteMemberAsync(long memberId, string modifiedBy);
    }
}
