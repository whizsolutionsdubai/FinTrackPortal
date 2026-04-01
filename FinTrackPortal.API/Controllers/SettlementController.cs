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

        public SettlementController(ISettlementService settlementService)
        {
            _settlementService = settlementService;
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
    }
}
