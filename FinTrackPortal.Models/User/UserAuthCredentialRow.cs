namespace FinTrackPortal.Models;

/// <summary>Used for password change — loaded via <c>sp_GetUserAuthByMemberId</c>.</summary>
public class UserAuthCredentialRow
{
    public long UserID { get; set; }
    public string EmailAddress { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
}
