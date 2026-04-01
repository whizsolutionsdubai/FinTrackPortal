using System.ComponentModel.DataAnnotations;

namespace FinTrackPortal.Models
{
    /// <summary>Request body for PUT /api/Member/edit.</summary>
    public class EditMemberRequest
    {
        [Required]
        public long MemberId { get; set; }

        [Required]
        [StringLength(100)]
        public string MemberName { get; set; } = string.Empty;
    }
}
