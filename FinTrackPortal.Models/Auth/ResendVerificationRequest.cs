using System.ComponentModel.DataAnnotations;

namespace FinTrackPortal.Models;

public class ResendVerificationRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
