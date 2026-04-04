using System.ComponentModel.DataAnnotations;

namespace FinTrackPortal.Models
{
    /// <summary>Request body for POST /api/Group/create. GroupCode is auto-generated server-side.</summary>
    public class CreateGroupRequest
    {
        [Required]
        [StringLength(150)]
        public string GroupName { get; set; } = string.Empty;
    }
}
