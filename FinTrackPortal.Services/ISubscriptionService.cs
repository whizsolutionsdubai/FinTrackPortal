using FinTrackPortal.Common;
using FinTrackPortal.Models;

namespace FinTrackPortal.Services
{
    /// <summary>
    /// Business-logic contract for subscription management.
    /// Includes plan listing, user subscription CRUD, and limit enforcement.
    /// </summary>
    public interface ISubscriptionService
    {
        Task<OperationResult<List<SubscriptionPlanResponse>>> GetPlansAsync();
        Task<OperationResult<UserSubscriptionResponse?>> GetUserSubscriptionAsync(long memberId);
        Task<OperationResult<bool>> CreateSubscriptionAsync(long memberId, int planId, string billingCycle, string? paymentRef);
        Task<OperationResult<bool>> CancelSubscriptionAsync(long memberId);
        Task<OperationResult<bool>> IsActionAllowedAsync(long memberId, string checkType, long? groupId = null);
    }
}
