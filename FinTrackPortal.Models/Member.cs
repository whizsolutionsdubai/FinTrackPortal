namespace FinTrackPortal.Models
{
    public class Member : BaseEntity
    {
        public long MemberId { get; set; }
        public string MemberName { get; set; } = string.Empty;
    }
}
