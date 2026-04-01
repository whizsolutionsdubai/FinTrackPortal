using System.ComponentModel.DataAnnotations;

namespace FinTrackPortal.Models
{
    public class CreateGroupRequest
    {
        [Required]
        [StringLength(150)]
        public string GroupName { get; set; } = string.Empty;
    }
}
