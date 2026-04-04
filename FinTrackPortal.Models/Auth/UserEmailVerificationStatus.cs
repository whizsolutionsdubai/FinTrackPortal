namespace FinTrackPortal.Models;

public sealed class UserEmailVerificationStatus
{
    public long MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public bool IsEmailVerified { get; set; }
}
