using System.ComponentModel.DataAnnotations;

namespace FinTrackPortal.Models
{
    /// <summary>Request body for POST /api/Group/add-member. Role defaults to "Member" if omitted.</summary>
    public class AddMemberRequest
    {
        [Required]
        public long GroupId { get; set; }

        [Required]
        public long MemberId { get; set; }

        [StringLength(20)]
        public string? Role { get; set; }
    }
}
