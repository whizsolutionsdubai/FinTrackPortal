namespace FinTrackPortal.Models
{
    /// <summary>Returned by GET /api/Settlement/group/{groupId}. Maps directly to sp_GetSettlementsByGroup result set.</summary>
    public class SettlementResponse
    {
        public long SettlementId { get; set; }
        public long GroupId { get; set; }
        public long FromMemberId { get; set; }
        public string FromMemberName { get; set; } = string.Empty;
        public long ToMemberId { get; set; }
        public string ToMemberName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime SettlementDate { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
