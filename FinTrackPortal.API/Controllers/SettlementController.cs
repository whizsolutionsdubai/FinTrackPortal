using FinTrackPortal.API.Extensions;
using FinTrackPortal.Common;
using FinTrackPortal.Models;
using FinTrackPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrackPortal.API.Controllers
{
    /// <summary>
    /// Settlement endpoints — record payments, view history, get suggested settlements.
    /// Suggested settlements are calculated in C# (not stored in DB).
    /// All endpoints require JWT authentication.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SettlementController : ControllerBase
    {
        private readonly ISettlementService _settlementService;
        private readonly IBankDetailsService _bankDetails;
        private readonly INotificationService _notifications;

        public SettlementController(
            ISettlementService settlementService,
            IBankDetailsService bankDetails,
            INotificationService notifications)
        {
            _settlementService = settlementService;
            _bankDetails = bankDetails;
            _notifications = notifications;
        }

        /// <summary>POST /api/Settlement/record — Record that one member paid another.</summary>
        [HttpPost("record")]
        public async Task<IActionResult> Record([FromBody] RecordSettlementRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object?>.ErrorResponse("Validation failed", errors));
            }

            var createdBy = User.GetEmail();
            var result = await _settlementService.RecordSettlementAsync(
                request.GroupId,
                request.FromMemberId,
                request.ToMemberId,
                request.Amount,
                createdBy);

            if (!result.IsSuccess)
                return BadRequest(ApiResponse<object?>.ErrorResponse(
                    "Failed to record settlement", result.ErrorMessage!));

            var fromName = User.GetEmail();
            try
            {
                await _notifications.CreateAsync(
                    request.ToMemberId,
                    "Settlement Received",
                    $"{fromName} marked a payment of {request.Amount:N2} to you",
                    "settlement",
                    "/settlement");
            }
            catch
            {
                // Notification delivery should never block settlement recording.
            }

            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                settlementId = result.Data
            }, "Settlement recorded successfully"));
        }

        /// <summary>GET /api/Settlement/group/{groupId} — Get settlement payment history for a group.</summary>
        [HttpGet("group/{groupId}")]
        public async Task<IActionResult> GetByGroup(long groupId)
        {
            var result = await _settlementService.GetSettlementsByGroupAsync(groupId);

            if (!result.IsSuccess)
                return BadRequest(ApiResponse<object?>.ErrorResponse(
                    "Failed to get settlements", result.ErrorMessage!));

            return Ok(ApiResponse<List<SettlementResponse>>.SuccessResponse(
                result.Data!, "Settlements retrieved successfully"));
        }

        /// <summary>GET /api/Settlement/suggested/{groupId} — Calculate optimal payments to settle all debts.</summary>
        [HttpGet("suggested/{groupId}")]
        public async Task<IActionResult> GetSuggested(long groupId)
        {
            var result = await _settlementService.GetSuggestedSettlementsAsync(groupId);

            if (!result.IsSuccess)
                return BadRequest(ApiResponse<object?>.ErrorResponse(
                    "Failed to get suggested settlements", result.ErrorMessage!));

            return Ok(ApiResponse<List<SuggestedSettlementResponse>>.SuccessResponse(
                result.Data!, "Suggested settlements calculated"));
        }

        /// <summary>
        /// GET /api/Settlement/{settlementId}/payee-bank-details — full IBAN for the payee; only the payer (FromMember) may call.
        /// </summary>
        [HttpGet("{settlementId:decimal}/payee-bank-details")]
        public async Task<IActionResult> GetPayeeBankDetails(decimal settlementId)
        {
            var memberId = User.GetMemberId();
            var result = await _bankDetails.GetPayeeBankForSettlementAsync(settlementId, memberId);
            if (!result.IsSuccess)
                return StatusCode(StatusCodes.Status403Forbidden,
                    ApiResponse<object?>.ErrorResponse(result.ErrorMessage ?? "Forbidden."));
            return Ok(ApiResponse<PayeeBankDetailsForSettlementResponse>.SuccessResponse(result.Data!, "OK"));
        }
    }
}
