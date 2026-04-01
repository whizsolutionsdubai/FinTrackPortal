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
    public class GroupController : ControllerBase
    {
        private readonly IGroupService _groupService;

        public GroupController(IGroupService groupService)
        {
            _groupService = groupService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateGroupRequest request)
        {
            var memberId = User.GetMemberId();
            var email = User.GetEmail();

            var result = await _groupService.CreateGroupAsync(request.GroupName, memberId, email);

            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(new { groupId = result.Data });
        }

        [HttpPost("add-member")]
        public async Task<IActionResult> AddMember([FromBody] AddMemberRequest request)
        {
            var createdBy = User.GetEmail();
            var result = await _groupService.AddMemberToGroupAsync(request.GroupId, request.MemberId, createdBy);

            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(new { message = "Member added successfully" });
        }

        [HttpGet("summary/{groupId}")]
        public async Task<IActionResult> Summary(long groupId)
        {
            var result = await _groupService.GetGroupSummaryAsync(groupId);

            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Data);
        }
    }
}
