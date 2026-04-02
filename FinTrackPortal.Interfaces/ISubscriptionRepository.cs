using FinTrackPortal.Common;
using FinTrackPortal.Models;

namespace FinTrackPortal.Interfaces
{
    /// <summary>
    /// Data-access contract for subscription plan management.
    /// Implemented by SubscriptionRepository using Dapper + stored procedures.
    /// </summary>
    public interface ISubscriptionRepository
    {
        /// <summary>Return all active plans for the pricing screen (sp_GetSubscriptionPlans).</summary>
        Task<OperationResult<List<SubscriptionPlanResponse>>> GetPlansAsync();

        /// <summary>Return the current active subscription for a member (sp_GetUserSubscription).</summary>
        Task<OperationResult<UserSubscriptionResponse?>> GetUserSubscriptionAsync(long memberId);

        /// <summary>Activate a subscription after payment (sp_CreateUserSubscription).</summary>
        Task<OperationResult<bool>> CreateSubscriptionAsync(long memberId, int planId, string billingCycle, string? paymentRef);

        /// <summary>Cancel a member's active subscription (sp_CancelUserSubscription).</summary>
        Task<OperationResult<bool>> CancelSubscriptionAsync(long memberId);

        /// <summary>Check if a member is within their plan's group or member-per-group limit (sp_CheckUserLimit).</summary>
        Task<OperationResult<bool>> IsActionAllowedAsync(long memberId, string checkType, long? groupId = null);
    }
}
