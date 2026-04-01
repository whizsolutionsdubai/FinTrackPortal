namespace FinTrackPortal.Models
{
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
