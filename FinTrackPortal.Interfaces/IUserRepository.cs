using FinTrackPortal.Common;

namespace FinTrackPortal.Interfaces
{
    /// <summary>
    /// Data-access contract for user authentication and registration.
    /// Implemented by UserRepository using Dapper + stored procedures.
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>Validate credentials against sp_ValidateUser. Returns the MemberId on success.</summary>
        Task<OperationResult<long>> ValidateUserAsync(string username, string password);

        /// <summary>Fetch account expiry date via sp_GetExpiry. Used during login to block expired accounts.</summary>
        Task<OperationResult<DateTime?>> GetUserExpiryAsync(string username);

        /// <summary>
        /// Register a new user via sp_RegisterUser.
        /// Creates a Member row and a User row inside a single SQL transaction.
        /// Returns the new MemberId.
        /// </summary>
        Task<OperationResult<long>> RegisterAsync(
            string memberName, string userName, string emailAddress,
            string? mobile, string password, string createdBy);
    }
}
