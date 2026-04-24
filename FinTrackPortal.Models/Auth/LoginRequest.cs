using System.ComponentModel.DataAnnotations;

namespace FinTrackPortal.Models
{
    /// <summary>Request body for POST /api/Auth/login.</summary>
    public class LoginRequest
    {
        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string Password { get; set; } = string.Empty;


        [Required]
        [StringLength(255)]
        public string OTP { get; set; } = string.Empty;



    }
}
