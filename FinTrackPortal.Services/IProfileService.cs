using FinTrackPortal.Common;
using FinTrackPortal.Models;

namespace FinTrackPortal.Services;

/// <summary>Logged-in user profile and password change.</summary>
public interface IProfileService
{
    Task<OperationResult<UserProfileResponse>> GetProfileAsync(long memberId);

    Task<OperationResult<UserProfileResponse>> UpdateProfileAsync(long memberId, UpdateProfileRequest request, string modifiedBy);

    Task<OperationResult<bool>> ChangePasswordAsync(long memberId, ChangePasswordRequest request, string modifiedBy);
}
