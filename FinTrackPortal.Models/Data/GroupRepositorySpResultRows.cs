namespace FinTrackPortal.Models;

/// <summary>
/// Flat row from <c>sp_GetGroupSummary</c> before mapping to <see cref="GroupSummaryResponse"/>.
/// </summary>
public sealed class GroupSummarySpRow
{
    public string GroupName { get; set; } = string.Empty;
    public string GroupCode { get; set; } = string.Empty;
    public long MemberId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Paid { get; set; }
    public decimal Share { get; set; }
    public decimal Net { get; set; }
}
