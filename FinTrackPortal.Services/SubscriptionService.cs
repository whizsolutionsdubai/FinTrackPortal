using FinTrackPortal.Common;
using FinTrackPortal.Interfaces;
using FinTrackPortal.Models;

namespace FinTrackPortal.Services
{
    /// <summary>Delegates all subscription operations to <see cref="ISubscriptionRepository"/>.</summary>
    public class SubscriptionService : ISubscriptionService
    {
        private readonly ISubscriptionRepository _repository;

        public SubscriptionService(ISubscriptionRepository repository)
        {
            _repository = repository;
        }

        public Task<OperationResult<List<SubscriptionPlanResponse>>> GetPlansAsync()
            => _repository.GetPlansAsync();

        public Task<OperationResult<UserSubscriptionResponse?>> GetUserSubscriptionAsync(long memberId)
            => _repository.GetUserSubscriptionAsync(memberId);

        public Task<OperationResult<bool>> CreateSubscriptionAsync(long memberId, int planId, string billingCycle, string? paymentRef)
            => _repository.CreateSubscriptionAsync(memberId, planId, billingCycle, paymentRef);

        public Task<OperationResult<bool>> CancelSubscriptionAsync(long memberId)
            => _repository.CancelSubscriptionAsync(memberId);

        public Task<OperationResult<bool>> IsActionAllowedAsync(long memberId, string checkType, long? groupId = null)
            => _repository.IsActionAllowedAsync(memberId, checkType, groupId);
    }
}
