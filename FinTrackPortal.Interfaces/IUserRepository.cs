using FinTrackPortal.Common;

namespace FinTrackPortal.Interfaces
{
    public interface IUserRepository
    {
        Task<OperationResult<long>> ValidateUserAsync(string username, string password);
        Task<OperationResult<DateTime?>> GetUserExpiryAsync(string username);
    }
}
