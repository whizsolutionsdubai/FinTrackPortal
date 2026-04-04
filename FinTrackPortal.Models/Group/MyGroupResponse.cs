namespace FinTrackPortal.Models
{
    /// <summary>Returned by GET /api/Group/my-groups. One entry per group the logged-in member belongs to.</summary>
    public class MyGroupResponse
    {
        public long GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string GroupCode { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }
}
