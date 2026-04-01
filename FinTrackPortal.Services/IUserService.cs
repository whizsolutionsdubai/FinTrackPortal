using FinTrackPortal.Common;

namespace FinTrackPortal.Services
{
    /// <summary>
    /// Business-logic contract for user authentication and registration.
    /// Currently a thin pass-through to <see cref="Interfaces.IUserRepository"/>.
    /// </summary>
    public interface IUserService
    {
        Task<OperationResult<long>> ValidateUserAsync(string username, string password);
        Task<OperationResult<DateTime?>> GetUserExpiryAsync(string username);
        Task<OperationResult<long>> RegisterAsync(
            string memberName, string userName, string emailAddress,
            string? mobile, string password, string createdBy);
    }
}
