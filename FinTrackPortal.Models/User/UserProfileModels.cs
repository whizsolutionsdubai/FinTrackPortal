using System.ComponentModel.DataAnnotations;

namespace FinTrackPortal.Models;

public class UserProfileResponse
{
    public long MemberId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class UpdateProfileRequest
{
    [Required]
    [StringLength(150, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [StringLength(255)]
    public string? PhoneNumber { get; set; }
}

public class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [StringLength(64, MinimumLength = 8)]
    public string NewPassword { get; set; } = string.Empty;
}
