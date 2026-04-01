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
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object?>.ErrorResponse("Validation failed", errors));
            }

            var memberId = User.GetMemberId();
            var email = User.GetEmail();

            var result = await _groupService.CreateGroupAsync(request.GroupName, memberId, email);

            if (!result.IsSuccess)
                return BadRequest(ApiResponse<object?>.ErrorResponse("Failed to create group", result.ErrorMessage!));

            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                groupId = result.Data
            }, "Group created successfully"));
        }

        [HttpPost("add-member")]
        public async Task<IActionResult> AddMember([FromBody] AddMemberRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object?>.ErrorResponse("Validation failed", errors));
            }

            var createdBy = User.GetEmail();
            string role = request.Role ?? "Member";

            var result = await _groupService.AddMemberToGroupAsync(request.GroupId, request.MemberId, role, createdBy);

            if (!result.IsSuccess)
                return BadRequest(ApiResponse<object?>.ErrorResponse("Failed to add member", result.ErrorMessage!));

            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                groupId = request.GroupId,
                memberId = request.MemberId,
                role
            }, "Member added successfully"));
        }

        [HttpGet("summary/{groupId}")]
        public async Task<IActionResult> Summary(long groupId)
        {
            var result = await _groupService.GetGroupSummaryAsync(groupId);

            if (!result.IsSuccess)
                return BadRequest(ApiResponse<object?>.ErrorResponse("Failed to get group summary", result.ErrorMessage!));

            return Ok(ApiResponse<GroupSummaryResponse>.SuccessResponse(result.Data!, "Group summary retrieved successfully"));
        }

        [HttpGet("my-groups")]
        public async Task<IActionResult> MyGroups()
        {
            var memberId = User.GetMemberId();
            var result = await _groupService.GetMyGroupsAsync(memberId);

            if (!result.IsSuccess)
                return BadRequest(ApiResponse<object?>.ErrorResponse("Failed to get groups", result.ErrorMessage!));

            return Ok(ApiResponse<List<MyGroupResponse>>.SuccessResponse(result.Data!, "Groups retrieved successfully"));
        }

        [HttpGet("{groupId}/members")]
        public async Task<IActionResult> GetMembers(long groupId)
        {
            var result = await _groupService.GetGroupMembersAsync(groupId);

            if (!result.IsSuccess)
                return BadRequest(ApiResponse<object?>.ErrorResponse("Failed to get group members", result.ErrorMessage!));

            return Ok(ApiResponse<List<GroupMemberResponse>>.SuccessResponse(result.Data!, "Group members retrieved successfully"));
        }
    }
}
