using FinTrackPortal.Common;
using FinTrackPortal.Interfaces;

namespace FinTrackPortal.Services
{
    /// <summary>Delegates all user operations to <see cref="IUserRepository"/>.</summary>
    public class UserService : IUserService
    {
        private readonly IUserRepository _repository;

        public UserService(IUserRepository repository)
        {
            _repository = repository;
        }

        public Task<OperationResult<long>> ValidateUserAsync(string username, string password)
           => _repository.ValidateUserAsync(username, password);

        public Task<OperationResult<DateTime?>> GetUserExpiryAsync(string username)
            => _repository.GetUserExpiryAsync(username);

        public Task<OperationResult<long>> RegisterAsync(
            string memberName, string userName, string emailAddress,
            string? mobile, string password, string createdBy)
            => _repository.RegisterAsync(memberName, userName, emailAddress, mobile, password, createdBy);
    }
}
