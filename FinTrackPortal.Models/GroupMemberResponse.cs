namespace FinTrackPortal.Models
{
    public class GroupMemberResponse
    {
        public long MemberId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}
