using FinTrackPortal.API.Extensions;
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
            var createdBy = User.GetEmail();

            var result = await _memberService.CreateMemberAsync(request.MemberName, createdBy);

            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(new { memberId = result.Data });
        }

        [HttpPut("edit")]
        public async Task<IActionResult> Edit([FromBody] EditMemberRequest request)
        {
            var modifiedBy = User.GetEmail();

            var result = await _memberService.EditMemberAsync(request.MemberId, request.MemberName, modifiedBy);

            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(new { message = "Member updated successfully" });
        }

        [HttpDelete("delete/{memberId}")]
        public async Task<IActionResult> Delete(long memberId)
        {
            var modifiedBy = User.GetEmail();

            var result = await _memberService.DeleteMemberAsync(memberId, modifiedBy);

            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(new { message = "Member deleted successfully" });
        }
    }
}
