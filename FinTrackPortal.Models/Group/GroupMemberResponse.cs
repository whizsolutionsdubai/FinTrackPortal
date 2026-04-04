namespace FinTrackPortal.Models
{
    /// <summary>Returned by GET /api/Group/{groupId}/members. Includes the member's role (Admin/Member).</summary>
    public class GroupMemberResponse
    {
        public long MemberId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}
