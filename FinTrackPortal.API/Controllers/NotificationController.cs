using FinTrackPortal.API.Extensions;
using FinTrackPortal.Common;
using FinTrackPortal.Models;
using FinTrackPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrackPortal.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notifications;

    public NotificationController(INotificationService notifications)
    {
        _notifications = notifications;
    }

    /// <summary>GET /api/Notification?unreadOnly=false&amp;take=100</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool unreadOnly = false, [FromQuery] int take = 100)
    {
        var memberId = User.GetMemberId();
        var items = await _notifications.GetAsync(memberId, unreadOnly, take);
        return Ok(ApiResponse<IReadOnlyList<NotificationItem>>.SuccessResponse(items, "OK"));
    }

    /// <summary>PUT /api/Notification/{notificationId}/read</summary>
    [HttpPut("{notificationId:long}/read")]
    public async Task<IActionResult> MarkRead(long notificationId)
    {
        var memberId = User.GetMemberId();
        var ok = await _notifications.MarkReadAsync(notificationId, memberId);
        if (!ok)
            return NotFound(ApiResponse<object?>.ErrorResponse("Notification not found."));
        return Ok(ApiResponse<object>.SuccessResponse(new { }, "Marked read"));
    }
}
