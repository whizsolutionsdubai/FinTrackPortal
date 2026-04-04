using FinTrackPortal.Common;
using FinTrackPortal.Interfaces;
using FinTrackPortal.Models;
using Microsoft.Extensions.Logging;

namespace FinTrackPortal.Services;

public sealed class ProfileService : IProfileService
{
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly ILogger<ProfileService> _logger;

    public ProfileService(
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        ILogger<ProfileService> logger)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _logger = logger;
    }

    public async Task<OperationResult<UserProfileResponse>> GetProfileAsync(long memberId)
    {
        var profile = await _users.GetUserProfileAsync(memberId);
        if (profile == null)
            return OperationResult<UserProfileResponse>.Failure("Profile not found.");
        return OperationResult<UserProfileResponse>.Success(profile);
    }

    public async Task<OperationResult<UserProfileResponse>> UpdateProfileAsync(long memberId, UpdateProfileRequest request, string modifiedBy)
    {
        var ok = await _users.UpdateUserProfileAsync(memberId, request.Name, request.PhoneNumber, modifiedBy);
        if (!ok)
            return OperationResult<UserProfileResponse>.Failure("Could not update profile.");

        var updated = await _users.GetUserProfileAsync(memberId);
        if (updated == null)
            return OperationResult<UserProfileResponse>.Failure("Profile not found after update.");

        return OperationResult<UserProfileResponse>.Success(updated);
    }

    public async Task<OperationResult<bool>> ChangePasswordAsync(long memberId, ChangePasswordRequest request, string modifiedBy)
    {
        var (valid, msg) = PasswordValidator.Validate(request.NewPassword);
        if (!valid)
            return OperationResult<bool>.Failure(msg);

        var auth = await _users.GetUserAuthByMemberIdAsync(memberId);
        if (auth == null || string.IsNullOrEmpty(auth.PasswordHash))
            return OperationResult<bool>.Failure("User not found.");

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, auth.PasswordHash))
            return OperationResult<bool>.Failure("Current password is incorrect.");

        if (BCrypt.Net.BCrypt.Verify(request.NewPassword, auth.PasswordHash))
            return OperationResult<bool>.Failure("New password must be different from the current password.");

        var newHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor: 12);
        var rows = await _users.UpdatePasswordHashByMemberIdAsync(memberId, newHash, modifiedBy);
        if (rows <= 0)
            return OperationResult<bool>.Failure("Could not update password.");

        try
        {
            await _refreshTokens.RevokeAllForMemberAsync(memberId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Password changed but failed to revoke refresh tokens for member {MemberId}", memberId);
        }

        return OperationResult<bool>.Success(true);
    }
}
