using FinTrackPortal.Common;

namespace FinTrackPortal.Interfaces
{
    /// <summary>
    /// Data-access contract for member profile CRUD.
    /// Implemented by MemberRepository using Dapper + stored procedures.
    /// Deletes are soft-deletes (IsActive = 0).
    /// </summary>
    public interface IMemberRepository
    {
        /// <summary>Create a new member profile (sp_CreateMember). Returns the new MemberId.</summary>
        Task<OperationResult<long>> CreateMemberAsync(string memberName, string createdBy);

        /// <summary>Update the member's name (sp_EditMember).</summary>
        Task<OperationResult<bool>> EditMemberAsync(long memberId, string memberName, string modifiedBy);

        /// <summary>Soft-delete a member by setting IsActive = 0 (sp_DeleteMember).</summary>
        Task<OperationResult<bool>> DeleteMemberAsync(long memberId, string modifiedBy);
    }
}
