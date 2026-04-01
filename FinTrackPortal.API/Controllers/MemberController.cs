using FinTrackPortal.API.Extensions;
using FinTrackPortal.Common;
using FinTrackPortal.Models;
using FinTrackPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrackPortal.API.Controllers
{
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
