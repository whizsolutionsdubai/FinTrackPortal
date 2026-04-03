using FinTrackPortal.Common;

namespace FinTrackPortal.Services
{
    /// <summary>User authentication, registration, email verification, and password reset.</summary>
    public interface IUserService
    {
        Task<OperationResult<long>> ValidateUserAsync(string username, string password);
        Task<OperationResult<DateTime?>> GetUserExpiryAsync(string username);
        Task<OperationResult<long>> RegisterAsync(
            string memberName, string userName, string emailAddress,
            string? mobile, string password, string createdBy);

        /// <summary>Returns MemberId on success.</summary>
        Task<OperationResult<long>> VerifyEmailAsync(string token);

        /// <summary>Always succeeds from API perspective; sends email only if account exists. Returns MemberId when the email was found.</summary>
        Task<long?> ForgotPasswordAsync(string email);

        /// <summary>Returns MemberId on successful reset.</summary>
        Task<OperationResult<long>> ResetPasswordAsync(string token, string newPassword);

        /// <summary>Resend verification email if the address exists and is not yet verified. Idempotent.</summary>
        Task ResendVerificationAsync(string email);
    }
}
