using FinTrackPortal.API.Extensions;
using FinTrackPortal.Common;
using FinTrackPortal.Models;
using FinTrackPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrackPortal.API.Controllers;

/// <summary>Cross-cutting expense + settlement activity for the current user.</summary>
[Route("api/[controller]")]
[ApiController]
[Authorize]
public class TransactionController : ControllerBase
{
    private readonly ITransactionService _transactions;

    public TransactionController(ITransactionService transactions)
    {
        _transactions = transactions;
    }

    /// <summary>GET /api/Transaction/history?skip=0&amp;take=50</summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        var memberId = User.GetMemberId();
        var result = await _transactions.GetHistoryWithSummaryAsync(memberId, skip, take);
        return Ok(ApiResponse<object>.SuccessResponse(new
        {
            summary = result.Data!.Summary,
            items = result.Data.Items
        }, "OK"));
    }
}
