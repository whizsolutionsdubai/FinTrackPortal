using System.ComponentModel.DataAnnotations;

namespace FinTrackPortal.Models
{
    /// <summary>Request body for POST /api/Member/create.</summary>
    public class CreateMemberRequest
    {
        [Required]
        [StringLength(100)]
        public string MemberName { get; set; } = string.Empty;
    }
}
