using FinTrackPortal.API.Extensions;
using FinTrackPortal.Common;
using FinTrackPortal.Models;
using FinTrackPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrackPortal.API.Controllers
{
    /// <summary>
    /// Group management endpoints — create, add members, summary, and listing.
    /// All endpoints require JWT authentication.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class GroupController : ControllerBase
    {
        private readonly IGroupService _groupService;
        private readonly ISubscriptionService _subscriptionService;

        public GroupController(IGroupService groupService, ISubscriptionService subscriptionService)
        {
            _groupService = groupService;
            _subscriptionService = subscriptionService;
        }

        /// <summary>
        /// POST /api/Group/create — Creates a group and adds the caller as Admin.
        /// Enforces the subscription plan's MaxGroups limit before proceeding.
        /// </summary>
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

            var limitCheck = await _subscriptionService.IsActionAllowedAsync(memberId, "group");
            if (limitCheck.IsSuccess && !limitCheck.Data)
                return StatusCode(402, ApiResponse<object?>.ErrorResponse(
                    "You have reached the maximum number of groups on your current plan.",
                    "Upgrade to Premium for unlimited groups."));

            var result = await _groupService.CreateGroupAsync(request.GroupName, memberId, email);

            if (!result.IsSuccess)
                return BadRequest(ApiResponse<object?>.ErrorResponse("Failed to create group", result.ErrorMessage!));

            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                groupId = result.Data
            }, "Group created successfully"));
        }

        /// <summary>
        /// POST /api/Group/add-member — Adds an existing member to a group.
        /// Enforces the subscription plan's MaxMembersPerGroup limit before proceeding.
        /// </summary>
        [HttpPost("add-member")]
        public async Task<IActionResult> AddMember([FromBody] AddMemberRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object?>.ErrorResponse("Validation failed", errors));
            }

            var createdBy = User.GetEmail();
            var memberId = User.GetMemberId();
            string role = request.Role ?? "Member";

            var limitCheck = await _subscriptionService.IsActionAllowedAsync(memberId, "member", request.GroupId);
            if (limitCheck.IsSuccess && !limitCheck.Data)
                return StatusCode(402, ApiResponse<object?>.ErrorResponse(
                    "You have reached the maximum members for this group on your current plan.",
                    "Upgrade to Premium for unlimited members per group."));

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

        /// <summary>GET /api/Group/summary/{groupId} — Per-member balance breakdown (paid, share, net).</summary>
        [HttpGet("summary/{groupId}")]
        public async Task<IActionResult> Summary(long groupId)
        {
            var result = await _groupService.GetGroupSummaryAsync(groupId);

            if (!result.IsSuccess)
                return BadRequest(ApiResponse<object?>.ErrorResponse("Failed to get group summary", result.ErrorMessage!));

            return Ok(ApiResponse<GroupSummaryResponse>.SuccessResponse(result.Data!, "Group summary retrieved successfully"));
        }

        /// <summary>GET /api/Group/my-groups — Lists all groups the logged-in member belongs to.</summary>
        [HttpGet("my-groups")]
        public async Task<IActionResult> MyGroups()
        {
            var memberId = User.GetMemberId();
            var result = await _groupService.GetMyGroupsAsync(memberId);

            if (!result.IsSuccess)
                return BadRequest(ApiResponse<object?>.ErrorResponse("Failed to get groups", result.ErrorMessage!));

            return Ok(ApiResponse<List<MyGroupResponse>>.SuccessResponse(result.Data!, "Groups retrieved successfully"));
        }

        /// <summary>GET /api/Group/{groupId}/members — Lists all members with roles.</summary>
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
