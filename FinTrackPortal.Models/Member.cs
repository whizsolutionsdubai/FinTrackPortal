namespace FinTrackPortal.Models
{
    /// <summary>
    /// Maps to the [dbo].[Member] table. This is the core identity —
    /// Users, GroupMembers, Expenses, and Settlements all reference MemberId.
    /// </summary>
    public class Member : BaseEntity
    {
        public long MemberId { get; set; }
        public string MemberName { get; set; } = string.Empty;
    }
}
