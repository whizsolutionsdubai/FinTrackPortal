using FinTrackPortal.Common;
using FinTrackPortal.Interfaces;

namespace FinTrackPortal.Services
{
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
    }
}
