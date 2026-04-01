using System.ComponentModel.DataAnnotations;

namespace FinTrackPortal.Models
{
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
