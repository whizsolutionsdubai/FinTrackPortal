using FinTrackPortal.API.Extensions;
using FinTrackPortal.Common;
using FinTrackPortal.Models;
using FinTrackPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrackPortal.API.Controllers
{
    /// <summary>
    /// Subscription management endpoints — plans, activate, cancel, and current subscription.
    /// Plans endpoint is public; all others require JWT authentication.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SubscriptionController : ControllerBase
    {
        private readonly ISubscriptionService _subscriptionService;

        public SubscriptionController(ISubscriptionService subscriptionService)
        {
            _subscriptionService = subscriptionService;
        }

        /// <summary>GET /api/Subscription/plans — Returns all active plans for the pricing screen.</summary>
        [HttpGet("plans")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPlans()
        {
            var result = await _subscriptionService.GetPlansAsync();

            if (!result.IsSuccess)
                return BadRequest(ApiResponse<object?>.ErrorResponse("Failed to get plans", result.ErrorMessage!));

            return Ok(ApiResponse<List<SubscriptionPlanResponse>>.SuccessResponse(result.Data!, "Plans retrieved successfully"));
        }

        /// <summary>GET /api/Subscription/my — Returns the current active subscription for the logged-in user.</summary>
        [HttpGet("my")]
        public async Task<IActionResult> GetMySubscription()
        {
            var memberId = User.GetMemberId();
            var result = await _subscriptionService.GetUserSubscriptionAsync(memberId);

            if (!result.IsSuccess)
                return BadRequest(ApiResponse<object?>.ErrorResponse("Failed to get subscription", result.ErrorMessage!));

            if (result.Data == null)
                return Ok(ApiResponse<object>.SuccessResponse(new { planName = "Free", message = "No active subscription. Free tier applies." }, "Free tier"));

            return Ok(ApiResponse<UserSubscriptionResponse>.SuccessResponse(result.Data, "Subscription retrieved successfully"));
        }

        /// <summary>
        /// GET /api/Subscription/user/{memberId} — Returns the current active subscription for a specific member.
        /// </summary>
        [HttpGet("user/{memberId}")]
        public async Task<IActionResult> GetUserSubscription(long memberId)
        {
            var result = await _subscriptionService.GetUserSubscriptionAsync(memberId);

            if (!result.IsSuccess)
                return BadRequest(ApiResponse<object?>.ErrorResponse("Failed to get subscription", result.ErrorMessage!));

            if (result.Data == null)
                return Ok(ApiResponse<object>.SuccessResponse(new { planName = "Free", message = "No active subscription. Free tier applies." }, "Free tier"));

            return Ok(ApiResponse<UserSubscriptionResponse>.SuccessResponse(result.Data, "Subscription retrieved successfully"));
        }

        /// <summary>POST /api/Subscription/activate — Activate a plan after payment gateway confirmation.</summary>
        [HttpPost("activate")]
        public async Task<IActionResult> Activate([FromBody] CreateSubscriptionRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object?>.ErrorResponse("Validation failed", errors));
            }

            var result = await _subscriptionService.CreateSubscriptionAsync(
                request.MemberId, request.PlanId, request.BillingCycle, request.PaymentRef);

            if (!result.IsSuccess)
                return BadRequest(ApiResponse<object?>.ErrorResponse("Failed to activate subscription", result.ErrorMessage!));

            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                memberId = request.MemberId,
                planId = request.PlanId
            }, "Subscription activated successfully"));
        }

        /// <summary>POST /api/Subscription/cancel — Cancel the logged-in user's active subscription.</summary>
        [HttpPost("cancel")]
        public async Task<IActionResult> Cancel()
        {
            var memberId = User.GetMemberId();

            var result = await _subscriptionService.CancelSubscriptionAsync(memberId);

            if (!result.IsSuccess)
                return BadRequest(ApiResponse<object?>.ErrorResponse("Failed to cancel subscription", result.ErrorMessage!));

            return Ok(ApiResponse<object>.SuccessResponse(new { memberId }, "Subscription cancelled successfully"));
        }
    }
}
