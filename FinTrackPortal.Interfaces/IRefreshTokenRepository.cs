namespace FinTrackPortal.Interfaces;

/// <summary>Opaque refresh tokens stored for JWT rotation (httpOnly cookie flow).</summary>
public interface IRefreshTokenRepository
{
    Task SaveAsync(long memberId, string token, DateTime expiresAtUtc);

    /// <summary>Returns member id if token exists, not revoked, and not expired.</summary>
    Task<long?> TryValidateMemberIdAsync(string token);

    Task RevokeAsync(string token);

    Task RevokeAllForMemberAsync(long memberId);

    Task<string?> GetEmailByMemberIdAsync(long memberId);
}
