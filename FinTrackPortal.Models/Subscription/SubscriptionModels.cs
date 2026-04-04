using System.ComponentModel.DataAnnotations;

namespace FinTrackPortal.Models
{
    /// <summary>
    /// Request body for POST /api/Subscription/activate.
    /// Call this after the payment gateway confirms payment.
    /// </summary>
    public class CreateSubscriptionRequest
    {
        [Required]
        public long MemberId { get; set; }

        [Required]
        public int PlanId { get; set; }

        /// <summary>"monthly" or "yearly".</summary>
        [Required]
        [StringLength(10)]
        public string BillingCycle { get; set; } = "monthly";

        /// <summary>Reference from the payment gateway (Stripe, etc.).</summary>
        [StringLength(200)]
        public string? PaymentRef { get; set; }
    }

    /// <summary>Plan definition returned for the pricing/upgrade screen.</summary>
    public class SubscriptionPlanResponse
    {
        public int PlanId { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public decimal MonthlyPrice { get; set; }
        public decimal YearlyPrice { get; set; }
        public int MaxGroups { get; set; }
        public int MaxMembersPerGroup { get; set; }
    }

    /// <summary>A user's current active subscription.</summary>
    public class UserSubscriptionResponse
    {
        public long SubscriptionId { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public int MaxGroups { get; set; }
        public int MaxMembersPerGroup { get; set; }
        public string BillingCycle { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool IsActive { get; set; }
    }
}
