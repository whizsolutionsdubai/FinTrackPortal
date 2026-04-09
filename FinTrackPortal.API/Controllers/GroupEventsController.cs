using FinTrackPortal.API.Extensions;
using FinTrackPortal.Common;
using FinTrackPortal.Models;
using FinTrackPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrackPortal.API.Controllers;

/// <summary>Group calendar events (server-side; replaces localStorage on clients).</summary>
[ApiController]
[Authorize]
[Route("api/Group/{groupId:long}/events")]
public class GroupEventsController : ControllerBase
{
    private readonly IGroupEventService _events;
    private readonly IGroupService _groups;
    private readonly IExpenseService _expenses;
    private readonly INotificationService _notifications;
    private readonly ILogger<GroupEventsController> _logger;

    public GroupEventsController(
        IGroupEventService events,
        IGroupService groups,
        IExpenseService expenses,
        INotificationService notifications,
        ILogger<GroupEventsController> logger)
    {
        _events = events;
        _groups = groups;
        _expenses = expenses;
        _notifications = notifications;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> List(long groupId)
    {
        var memberId = User.GetMemberId();
        var result = await _events.GetAsync(groupId, memberId);
        if (!result.IsSuccess)
            return BadRequest(ApiResponse<object?>.ErrorResponse(result.ErrorMessage!));
        return Ok(ApiResponse<IReadOnlyList<GroupEventResponse>>.SuccessResponse(result.Data!, "OK"));
    }

    [HttpPost]
    public async Task<IActionResult> Create(long groupId, [FromBody] CreateGroupEventRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<object?>.ErrorResponse("Validation failed", errors));
        }

        var memberId = User.GetMemberId();
        var result = await _events.CreateAsync(groupId, memberId, request);
        if (!result.IsSuccess)
            return BadRequest(ApiResponse<object?>.ErrorResponse(result.ErrorMessage!));

        await TryNotifyGroup(
            groupId,
            memberId,
            "New Event Created",
            $"New event: {request.Title} - {request.EventDate:yyyy-MM-dd HH:mm}",
            "event",
            $"/group/{groupId}/events/{result.Data}");

        return Ok(ApiResponse<object>.SuccessResponse(new { eventId = result.Data }, "Event created"));
    }

    [HttpPut("{eventId:long}")]
    public async Task<IActionResult> Update(long groupId, long eventId, [FromBody] UpdateGroupEventRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<object?>.ErrorResponse("Validation failed", errors));
        }

        var memberId = User.GetMemberId();
        var before = await _events.GetAsync(groupId, memberId);
        var oldEvent = before.IsSuccess ? before.Data?.FirstOrDefault(e => e.EventId == eventId) : null;
        var result = await _events.UpdateAsync(groupId, eventId, memberId, request);
        if (!result.IsSuccess)
            return BadRequest(ApiResponse<object?>.ErrorResponse(result.ErrorMessage!));

        var changedDateOrLocation = oldEvent != null &&
                                    (oldEvent.EventDate != request.EventDate ||
                                     !string.Equals(oldEvent.Location, request.Location, StringComparison.Ordinal));
        if (changedDateOrLocation)
        {
            await TryNotifyGroup(
                groupId,
                memberId,
                "Event Updated",
                $"Event updated: {request.Title}",
                "event",
                $"/group/{groupId}/events/{eventId}");
        }

        return Ok(ApiResponse<object>.SuccessResponse(new { }, "Event updated"));
    }

    [HttpDelete("{eventId:long}")]
    public async Task<IActionResult> Delete(long groupId, long eventId)
    {
        var memberId = User.GetMemberId();
        var result = await _events.DeleteAsync(groupId, eventId, memberId);
        if (!result.IsSuccess)
            return BadRequest(ApiResponse<object?>.ErrorResponse(result.ErrorMessage!));
        return Ok(ApiResponse<object>.SuccessResponse(new { }, "Event deleted"));
    }

    [HttpGet("{eventId:long}/expenses")]
    public async Task<IActionResult> GetEventExpenses(long groupId, long eventId)
    {
        var memberId = User.GetMemberId();
        var memberCheck = await _groups.IsMemberOfGroupAsync(groupId, memberId);
        if (!memberCheck.IsSuccess || !memberCheck.Data)
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object?>.ErrorResponse("You must belong to the group to view event expenses."));

        var result = await _expenses.GetEventExpensesAsync(groupId, eventId);
        if (!result.IsSuccess)
            return BadRequest(ApiResponse<object?>.ErrorResponse(result.ErrorMessage!));
        return Ok(ApiResponse<List<EventExpenseSummaryResponse>>.SuccessResponse(result.Data!, "OK"));
    }

    private async Task TryNotifyGroup(long groupId, long actorMemberId, string title, string body, string type, string linkUrl)
    {
        try
        {
            var members = await _groups.GetGroupMembersAsync(groupId);
            if (!members.IsSuccess || members.Data == null) return;
            foreach (var member in members.Data.Where(m => m.MemberId != actorMemberId))
                await _notifications.CreateAsync(member.MemberId, title, body, type, linkUrl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Group event notification failed");
        }
    }
}
