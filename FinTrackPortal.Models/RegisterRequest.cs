using System.ComponentModel.DataAnnotations;

namespace FinTrackPortal.Models
{
    /// <summary>
    /// Request body for POST /api/Auth/register.
    /// Creates both a Member and a User record via sp_RegisterUser.
    /// </summary>
    public class RegisterRequest
    {
        [Required]
        [StringLength(150)]
        public string MemberName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string EmailAddress { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Mobile { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 8)]
        public string Password { get; set; } = string.Empty;
    }
}
