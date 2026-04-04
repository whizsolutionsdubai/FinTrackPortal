using FinTrackPortal.API.Extensions;
using FinTrackPortal.API.Services;
using FinTrackPortal.Common;
using FinTrackPortal.Interfaces;
using FinTrackPortal.Models;
using FinTrackPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrackPortal.API.Controllers;

/// <summary>Current user profile, password, bank details (IBAN).</summary>
[Route("api/[controller]")]
[ApiController]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserRepository _users;
    private readonly IProfileService _profile;
    private readonly IProfilePhotoService _profilePhoto;
    private readonly IBankDetailsService _bankDetails;

    public UserController(
        IUserRepository users,
        IProfileService profile,
        IProfilePhotoService profilePhoto,
        IBankDetailsService bankDetails)
    {
        _users = users;
        _profile = profile;
        _profilePhoto = profilePhoto;
        _bankDetails = bankDetails;
    }

    /// <summary>GET /api/User/profile</summary>
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var memberId = User.GetMemberId();
        var result = await _profile.GetProfileAsync(memberId);
        if (!result.IsSuccess)
            return NotFound(ApiResponse<object?>.ErrorResponse(result.ErrorMessage!));
        return Ok(ApiResponse<UserProfileResponse>.SuccessResponse(result.Data!, "OK"));
    }

    /// <summary>PUT /api/User/profile — name and phone; email change not supported.</summary>
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<object?>.ErrorResponse("Validation failed", errors));
        }

        var memberId = User.GetMemberId();
        var modifiedBy = User.GetEmail();
        var result = await _profile.UpdateProfileAsync(memberId, request, modifiedBy);
        if (!result.IsSuccess)
            return BadRequest(ApiResponse<object?>.ErrorResponse("Update failed", result.ErrorMessage!));
        return Ok(ApiResponse<UserProfileResponse>.SuccessResponse(result.Data!, "Profile updated"));
    }

    /// <summary>POST /api/User/profile/photo — multipart form field <c>photo</c>; stored as 200×200 JPEG.</summary>
    [HttpPost("profile/photo")]
    [RequestSizeLimit(5_242_880)]
    public async Task<IActionResult> UploadProfilePhoto(IFormFile? photo, CancellationToken cancellationToken)
    {
        if (photo == null || photo.Length == 0)
            return BadRequest(ApiResponse<object?>.ErrorResponse("Form field 'photo' is required."));

        var memberId = User.GetMemberId();
        var modifiedBy = User.GetEmail();

        var existing = await _users.GetUserProfileAsync(memberId);
        var oldUrl = existing?.ProfilePhotoUrl;

        var (ok, urlOrErr) = await _profilePhoto.SaveProfilePhotoAsync(memberId, photo, cancellationToken);
        if (!ok)
            return BadRequest(ApiResponse<object?>.ErrorResponse(urlOrErr));

        var rows = await _users.UpdateProfilePhotoUrlAsync(memberId, urlOrErr, modifiedBy);
        if (rows <= 0)
        {
            await _profilePhoto.TryDeleteByUrlAsync(urlOrErr, cancellationToken);
            return BadRequest(ApiResponse<object?>.ErrorResponse("Could not save profile photo URL."));
        }

        if (!string.IsNullOrEmpty(oldUrl) && !string.Equals(oldUrl, urlOrErr, StringComparison.OrdinalIgnoreCase))
            await _profilePhoto.TryDeleteByUrlAsync(oldUrl, cancellationToken);

        var updated = await _users.GetUserProfileAsync(memberId);
        return Ok(ApiResponse<UserProfileResponse>.SuccessResponse(updated!, "Photo updated"));
    }

    /// <summary>PUT /api/User/change-password — revokes all refresh tokens on success.</summary>
    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<object?>.ErrorResponse("Validation failed", errors));
        }

        var memberId = User.GetMemberId();
        var modifiedBy = User.GetEmail();
        var result = await _profile.ChangePasswordAsync(memberId, request, modifiedBy);
        if (!result.IsSuccess)
            return BadRequest(ApiResponse<object?>.ErrorResponse(result.ErrorMessage!));
        return Ok(ApiResponse<object>.SuccessResponse(new { }, "Password changed"));
    }

    /// <summary>GET /api/User/bank-details — masked IBAN only.</summary>
    [HttpGet("bank-details")]
    public async Task<IActionResult> GetBankDetails()
    {
        var memberId = User.GetMemberId();
        var result = await _bankDetails.GetAsync(memberId);
        if (!result.IsSuccess)
            return BadRequest(ApiResponse<object?>.ErrorResponse(result.ErrorMessage!));
        return Ok(ApiResponse<BankDetailsResponse?>.SuccessResponse(result.Data, "OK"));
    }

    /// <summary>PUT /api/User/bank-details</summary>
    [HttpPut("bank-details")]
    public async Task<IActionResult> SaveBankDetails([FromBody] SaveBankDetailsRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<object?>.ErrorResponse("Validation failed", errors));
        }

        var memberId = User.GetMemberId();
        var result = await _bankDetails.SaveAsync(memberId, request);
        if (!result.IsSuccess)
            return BadRequest(ApiResponse<object?>.ErrorResponse(result.ErrorMessage!));
        return Ok(ApiResponse<object>.SuccessResponse(new { }, "Bank details saved"));
    }

    /// <summary>DELETE /api/User/bank-details</summary>
    [HttpDelete("bank-details")]
    public async Task<IActionResult> DeleteBankDetails()
    {
        var memberId = User.GetMemberId();
        var result = await _bankDetails.DeleteAsync(memberId);
        if (!result.IsSuccess)
            return BadRequest(ApiResponse<object?>.ErrorResponse(result.ErrorMessage!));
        return Ok(ApiResponse<object>.SuccessResponse(new { }, "Bank details removed"));
    }
}
