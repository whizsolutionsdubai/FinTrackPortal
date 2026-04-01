using System.ComponentModel.DataAnnotations;

namespace FinTrackPortal.Models
{
    public class CreateMemberRequest
    {
        [Required]
        [StringLength(100)]
        public string MemberName { get; set; } = string.Empty;
    }
}
