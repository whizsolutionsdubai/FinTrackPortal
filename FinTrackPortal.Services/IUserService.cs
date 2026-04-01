using FinTrackPortal.Common;

namespace FinTrackPortal.Services
{
    public interface IUserService
    {
        Task<OperationResult<long>> ValidateUserAsync(string username, string password);
        Task<OperationResult<DateTime?>> GetUserExpiryAsync(string username);
    }
}
