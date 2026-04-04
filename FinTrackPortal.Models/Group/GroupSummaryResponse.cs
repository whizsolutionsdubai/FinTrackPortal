namespace FinTrackPortal.Models
{
    /// <summary>
    /// Returned by GET /api/Group/summary/{groupId}.
    /// Contains per-member balances: Paid (total paid), Share (total owed), Net (Paid - Share).
    /// Positive Net = member is owed money; negative Net = member owes money.
    /// </summary>
    public class GroupSummaryResponse
    {
        public long GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string GroupCode { get; set; } = string.Empty;
        public List<MemberSummary> Members { get; set; } = new();
    }

    public class MemberSummary
    {
        public long MemberId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Paid { get; set; }
        public decimal Share { get; set; }
        public decimal Net { get; set; }
    }
}
