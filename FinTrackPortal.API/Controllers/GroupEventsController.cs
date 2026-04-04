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

    public GroupEventsController(IGroupEventService events)
    {
        _events = events;
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
        var result = await _events.UpdateAsync(groupId, eventId, memberId, request);
        if (!result.IsSuccess)
            return BadRequest(ApiResponse<object?>.ErrorResponse(result.ErrorMessage!));
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
}
