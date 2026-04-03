using FinTrackPortal.Common;
using FinTrackPortal.Models;

namespace FinTrackPortal.Interfaces
{
    /// <summary>
    /// Data-access contract for user authentication and registration.
    /// Implemented by UserRepository using Dapper + stored procedures.
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>Validate credentials via <c>sp_ValidateUser</c>; returns MemberId on success.</summary>
        Task<OperationResult<long>> ValidateUserAsync(string username, string password);

        /// <summary>Fetch account expiry date via <c>sp_GetExpiry</c>. Used during login to block expired accounts.</summary>
        Task<OperationResult<DateTime?>> GetUserExpiryAsync(string username);

        /// <summary>
        /// Register a new user via <c>sp_RegisterUser</c>.
        /// Creates a Member row and a User row inside a single SQL transaction.
        /// Returns the new MemberId.
        /// </summary>
        Task<OperationResult<long>> RegisterAsync(
            string memberName, string userName, string emailAddress,
            string? mobile, string password, string createdBy);

        Task SaveEmailVerifyTokenAsync(string email, string token, int expiryHours);

        /// <summary>Verify email token; returns MemberId on success.</summary>
        Task<OperationResult<long>> VerifyEmailWithTokenAsync(string token);

        /// <summary>Member display name and id when the email is registered.</summary>
        Task<MemberEmailLookup?> LookupMemberByEmailAsync(string email);

        /// <summary>For resend verification — includes <see cref="UserEmailVerificationStatus.IsEmailVerified"/>.</summary>
        Task<UserEmailVerificationStatus?> GetUserEmailVerificationStatusAsync(string email);

        Task SavePasswordResetTokenAsync(string email, string token, DateTime expiryUtc);

        /// <summary>Hashes <paramref name="newPlainPassword"/> and updates row if token is valid and not expired. Returns MemberId on success.</summary>
        Task<OperationResult<long>> ResetPasswordWithTokenAsync(string token, string newPlainPassword);
    }
}
