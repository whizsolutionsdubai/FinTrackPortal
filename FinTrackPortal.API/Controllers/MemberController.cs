using FinTrackPortal.API.Extensions;
using FinTrackPortal.Common;
using FinTrackPortal.Models;
using FinTrackPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrackPortal.API.Controllers
{
    /// <summary>
    /// Member profile CRUD endpoints — create, edit, delete.
    /// Deletes are soft-deletes (IsActive = 0). All endpoints require JWT authentication.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MemberController : ControllerBase
    {
        private readonly IMemberService _memberService;

        public MemberController(IMemberService memberService)
        {
            _memberService = memberService;
        }

        /// <summary>POST /api/Member/create — Create a new member profile.</summary>
        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateMemberRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object?>.ErrorResponse("Validation failed", errors));
            }

            var createdBy = User.GetEmail();

            var result = await _memberService.CreateMemberAsync(request.MemberName, createdBy);

            if (!result.IsSuccess)
                return BadRequest(ApiResponse<object?>.ErrorResponse("Failed to create member", result.ErrorMessage!));

            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                memberId = result.Data,
                memberName = request.MemberName
            }, "Member created successfully"));
        }

        /// <summary>PUT /api/Member/edit — Update a member's name.</summary>
        [HttpPut("edit")]
        public async Task<IActionResult> Edit([FromBody] EditMemberRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object?>.ErrorResponse("Validation failed", errors));
            }

            var modifiedBy = User.GetEmail();

            var result = await _memberService.EditMemberAsync(request.MemberId, request.MemberName, modifiedBy);

            if (!result.IsSuccess)
                return BadRequest(ApiResponse<object?>.ErrorResponse("Failed to update member", result.ErrorMessage!));

            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                memberId = request.MemberId,
                memberName = request.MemberName
            }, "Member updated successfully"));
        }

        /// <summary>DELETE /api/Member/delete/{memberId} — Soft-delete a member.</summary>
        [HttpDelete("delete/{memberId}")]
        public async Task<IActionResult> Delete(long memberId)
        {
            var modifiedBy = User.GetEmail();

            var result = await _memberService.DeleteMemberAsync(memberId, modifiedBy);

            if (!result.IsSuccess)
                return BadRequest(ApiResponse<object?>.ErrorResponse("Failed to delete member", result.ErrorMessage!));

            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                memberId
            }, "Member deleted successfully"));
        }
    }
}
