namespace FinTrackPortal.Models
{
    /// <summary>
    /// Returned by GET /api/Settlement/suggested/{groupId}.
    /// Calculated in C# from group summary balances — not stored in the database.
    /// </summary>
    public class SuggestedSettlementResponse
    {
        public long FromMemberId { get; set; }
        public string FromMemberName { get; set; } = string.Empty;
        public long ToMemberId { get; set; }
        public string ToMemberName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}
