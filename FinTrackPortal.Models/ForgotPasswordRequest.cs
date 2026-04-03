using System.ComponentModel.DataAnnotations;

namespace FinTrackPortal.Models
{
    public class ForgotPasswordRequest
    {
        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;
    }
}
