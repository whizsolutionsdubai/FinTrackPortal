namespace FinTrackPortal.Models;

/// <summary>
/// Dapper row shapes for stored procedures used by <c>UserRepository</c>.
/// These mirror SQL result columns — they are not exposed as API contracts from controllers.
/// </summary>
public sealed class UserValidateUserSpRow
{
    public long MemberId { get; set; }
    public string? PasswordHash { get; set; }
    public bool IsEmailVerified { get; set; }
    public DateTime? LockoutUntil { get; set; }
    public int FailedLoginCount { get; set; }
}

public sealed class UserVerifyEmailSpRow
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public long? MemberId { get; set; }
}

public sealed class UserResetPasswordSpRow
{
    public int RowsUpdated { get; set; }
    public long? MemberId { get; set; }
}

public sealed class UserUpdateProfileSpRow
{
    public bool Success { get; set; }
}
