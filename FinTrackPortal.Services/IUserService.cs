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

        Task<OperationResult<bool>> VerifyEmailAsync(string token);

        /// <summary>Always succeeds from API perspective; sends email only if account exists.</summary>
        Task ForgotPasswordAsync(string email);

        Task<OperationResult<bool>> ResetPasswordAsync(string token, string newPassword);
    }
}
