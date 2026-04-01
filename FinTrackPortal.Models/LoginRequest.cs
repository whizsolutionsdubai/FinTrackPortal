using System.ComponentModel.DataAnnotations;

namespace FinTrackPortal.Models
{
    public class LoginRequest
    {
        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string Password { get; set; } = string.Empty;
    }
}
