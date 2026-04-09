using FinTrackPortal.API.Extensions;
using FinTrackPortal.Common;
using FinTrackPortal.Interfaces;
using FinTrackPortal.Models;
using FinTrackPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrackPortal.API.Controllers;

[ApiController]
[Authorize]
[Route("api/group/{groupId:long}/events/{eventId:long}/checklist")]
public class ChecklistController : ControllerBase
{
    private readonly IGroupChecklistService _checklist;
    private readonly IGroupService _groups;
    private readonly INotificationService _notifications;
    private readonly IAuditLogRepository _audit;
    private readonly ILogger<ChecklistController> _logger;

    public ChecklistController(
        IGroupChecklistService checklist,
        IGroupService groups,
        INotificationService notifications,
        IAuditLogRepository audit,
        ILogger<ChecklistController> logger)
    {
        _checklist = checklist;
        _groups = groups;
        _notifications = notifications;
        _audit = audit;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get(long groupId, long eventId)
    {
        var memberId = User.GetMemberId();
        var result = await _checklist.GetChecklistAsync(eventId, memberId);
        if (!result.IsSuccess)
            return BadRequest(ApiResponse<object?>.ErrorResponse(result.ErrorMessage!));
        return Ok(ApiResponse<IReadOnlyList<EventChecklistItemResponse>>.SuccessResponse(result.Data!, "OK"));
    }

    [HttpPost]
    public async Task<IActionResult> Add(long groupId, long eventId, [FromBody] AddChecklistItemRequest request)
    {
        var memberId = User.GetMemberId();
        if (!await IsOrganizerAsync(groupId, memberId))
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object?>.ErrorResponse("Only group organizers can add checklist items."));

        var result = await _checklist.AddItemAsync(eventId, request);
        if (!result.IsSuccess)
            return BadRequest(ApiResponse<object?>.ErrorResponse(result.ErrorMessage!));

        await _audit.WriteAsync(memberId, AuditChecklistActions.ChecklistItemCreated, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString(), true, $"EventId={eventId}, ChecklistItemId={result.Data}, Item='{request.ItemName}'");
        await TryNotifyGroup(groupId, memberId, "New Checklist Item", $"New item added to checklist: {request.ItemName}", "checklist", $"/group/{groupId}/events/{eventId}");

        return Ok(ApiResponse<object>.SuccessResponse(new { checklistItemId = result.Data }, "Checklist item added"));
    }

    [HttpPut("{itemId:long}")]
    public async Task<IActionResult> Update(long groupId, long eventId, long itemId, [FromBody] UpdateChecklistItemRequest request)
    {
        var memberId = User.GetMemberId();
        if (!await IsOrganizerAsync(groupId, memberId))
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object?>.ErrorResponse("Only group organizers can update checklist items."));

        var result = await _checklist.UpdateItemAsync(itemId, request);
        if (!result.IsSuccess)
            return BadRequest(ApiResponse<object?>.ErrorResponse(result.ErrorMessage!));

        var action = string.Equals(request.Status, "Completed", StringComparison.OrdinalIgnoreCase)
            ? AuditChecklistActions.ChecklistItemCompleted
            : AuditChecklistActions.ChecklistItemUpdated;
        await _audit.WriteAsync(memberId, action, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString(), true, $"EventId={eventId}, ChecklistItemId={itemId}");

        return Ok(ApiResponse<bool>.SuccessResponse(true, "Checklist item updated"));
    }

    [HttpDelete("{itemId:long}")]
    public async Task<IActionResult> Delete(long groupId, long eventId, long itemId)
    {
        var memberId = User.GetMemberId();
        if (!await IsOrganizerAsync(groupId, memberId))
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object?>.ErrorResponse("Only group organizers can delete checklist items."));

        var result = await _checklist.DeleteItemAsync(itemId);
        if (!result.IsSuccess)
            return BadRequest(ApiResponse<object?>.ErrorResponse(result.ErrorMessage!));

        await _audit.WriteAsync(memberId, AuditChecklistActions.ChecklistItemDeleted, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString(), true, $"EventId={eventId}, ChecklistItemId={itemId}");
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Checklist item deleted"));
    }

    [HttpPost("{itemId:long}/claim")]
    public async Task<IActionResult> Claim(long groupId, long eventId, long itemId, [FromBody] ClaimChecklistItemRequest request)
    {
        var memberId = User.GetMemberId();
        var before = await _checklist.GetChecklistAsync(eventId, memberId);
        var item = before.IsSuccess ? before.Data?.FirstOrDefault(x => x.ChecklistItemId == itemId) : null;
        var result = await _checklist.ClaimItemAsync(itemId, memberId, request.QuantityClaimed);
        if (!result.IsSuccess)
            return BadRequest(ApiResponse<object?>.ErrorResponse(result.ErrorMessage!));

        await _audit.WriteAsync(memberId, AuditChecklistActions.ChecklistClaimAdded, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString(), true, $"ChecklistItemId={itemId}, EventId={eventId}, Qty={request.QuantityClaimed}");
        var memberName = User.GetEmail();
        var itemName = item?.ItemName ?? $"Item #{itemId}";
        var unit = string.IsNullOrWhiteSpace(item?.QuantityUnit) ? string.Empty : $" {item.QuantityUnit}";
        await TryNotifyGroup(groupId, memberId, "Checklist Item Claimed", $"{memberName} claimed: {itemName} ({request.QuantityClaimed}{unit})", "checklist", $"/group/{groupId}/events/{eventId}");

        return Ok(ApiResponse<bool>.SuccessResponse(true, "Claimed successfully"));
    }

    [HttpDelete("{itemId:long}/claim")]
    public async Task<IActionResult> Withdraw(long groupId, long eventId, long itemId)
    {
        var memberId = User.GetMemberId();
        var before = await _checklist.GetChecklistAsync(eventId, memberId);
        var item = before.IsSuccess ? before.Data?.FirstOrDefault(x => x.ChecklistItemId == itemId) : null;
        var result = await _checklist.WithdrawClaimAsync(itemId, memberId);
        if (!result.IsSuccess)
            return BadRequest(ApiResponse<object?>.ErrorResponse(result.ErrorMessage!));

        await _audit.WriteAsync(memberId, AuditChecklistActions.ChecklistClaimWithdrawn, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString(), true, $"ChecklistItemId={itemId}, EventId={eventId}");
        var memberName = User.GetEmail();
        var itemName = item?.ItemName ?? $"Item #{itemId}";
        await TryNotifyGroup(groupId, memberId, "Claim Removed", $"{memberName} removed their claim for {itemName}", "checklist", $"/group/{groupId}/events/{eventId}");

        return Ok(ApiResponse<bool>.SuccessResponse(true, "Claim withdrawn"));
    }

    [HttpPut("{itemId:long}/complete")]
    public async Task<IActionResult> MarkComplete(long groupId, long eventId, long itemId)
    {
        var memberId = User.GetMemberId();
        var result = await _checklist.MarkCompleteAsync(groupId, eventId, itemId, memberId);
        if (!result.IsSuccess)
            return BadRequest(ApiResponse<object?>.ErrorResponse(result.ErrorMessage!));

        await _audit.WriteAsync(
            memberId,
            AuditChecklistActions.ChecklistItemCompleted,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(),
            true,
            $"EventId={eventId}, ChecklistItemId={itemId}");

        return Ok(ApiResponse<bool>.SuccessResponse(true, "Checklist item marked as completed"));
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
            _logger.LogWarning(ex, "Checklist notification failed");
        }
    }

    private async Task<bool> IsOrganizerAsync(long groupId, long memberId)
    {
        var members = await _groups.GetGroupMembersAsync(groupId);
        if (!members.IsSuccess || members.Data == null)
            return false;

        return members.Data.Any(m =>
            m.MemberId == memberId &&
            string.Equals(m.Role, "Admin", StringComparison.OrdinalIgnoreCase));
    }
}
