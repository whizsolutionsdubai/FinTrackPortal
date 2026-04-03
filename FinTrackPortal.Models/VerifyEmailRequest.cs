using System.ComponentModel.DataAnnotations;

namespace FinTrackPortal.Models
{
    public class VerifyEmailRequest
    {
        [Required]
        [StringLength(200)]
        public string Token { get; set; } = string.Empty;
    }
}
