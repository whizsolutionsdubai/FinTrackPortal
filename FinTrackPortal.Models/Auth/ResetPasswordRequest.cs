using System.ComponentModel.DataAnnotations;

namespace FinTrackPortal.Models
{
    public class ResetPasswordRequest
    {
        [Required]
        [StringLength(200)]
        public string Token { get; set; } = string.Empty;

        [Required]
        public string NewPassword { get; set; } = string.Empty;
    }
}
