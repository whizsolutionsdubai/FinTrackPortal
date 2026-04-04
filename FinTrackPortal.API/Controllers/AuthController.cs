using FinTrackPortal.API.Helpers;
using FinTrackPortal.Common;
using FinTrackPortal.Interfaces;
using FinTrackPortal.Models;
using FinTrackPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace FinTrackPortal.API.Controllers
{
    /// <summary>
    /// Authentication — login, registration, email verification, forgot/reset password, JWT refresh (httpOnly cookie).
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        public const string RefreshTokenCookieName = "finshare_refresh";

        private readonly JwtSettings _jwtSettings;
        private readonly IUserService _userService;
        private readonly IAuditLogRepository _auditLog;
        private readonly IRefreshTokenRepository _refreshTokens;

        public AuthController(
            IOptions<JwtSettings> jwtOptions,
            IUserService userService,
            IAuditLogRepository auditLog,
            IRefreshTokenRepository refreshTokens)
        {
            _jwtSettings = jwtOptions.Value;
            _userService = userService;
            _auditLog = auditLog;
            _refreshTokens = refreshTokens;
        }

        /// <summary>POST /api/Auth/login — JWT after email verification and valid credentials; sets httpOnly refresh cookie.</summary>
        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest login)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object?>.ErrorResponse("Validation failed", errors));
            }

            var validationResult = await _userService.ValidateUserAsync(login.UserName, login.Password);
            if (!validationResult.IsSuccess)
            {
                var locked = validationResult.ErrorMessage?.Contains("temporarily locked", StringComparison.OrdinalIgnoreCase) == true;
                await _auditLog.WriteAsync(
                    null,
                    locked ? AuditAuthActions.LoginBlocked : AuditAuthActions.LoginFailed,
                    IpHelper.GetClientIp(HttpContext),
                    GetUserAgent(),
                    false,
                    locked ? validationResult.ErrorMessage : "Invalid credentials");
                return Unauthorized(ApiResponse<object?>.ErrorResponse(validationResult.ErrorMessage ?? "Invalid credentials"));
            }

            var memberId = validationResult.Data;

            var expiryResult = await _userService.GetUserExpiryAsync(login.UserName);
            if (!expiryResult.IsSuccess || !expiryResult.Data.HasValue)
            {
                await _auditLog.WriteAsync(memberId, AuditAuthActions.LoginFailed, IpHelper.GetClientIp(HttpContext), GetUserAgent(), false, "Unable to retrieve account expiry");
                return Unauthorized(ApiResponse<object?>.ErrorResponse("Unable to retrieve account expiry"));
            }

            if (expiryResult.Data.Value < DateTime.UtcNow)
            {
                await _auditLog.WriteAsync(memberId, AuditAuthActions.LoginFailed, IpHelper.GetClientIp(HttpContext), GetUserAgent(), false, "Account expired");
                return Unauthorized(ApiResponse<object?>.ErrorResponse("Account has expired"));
            }

            var jwt = CreateAccessToken(memberId, login.UserName);
            var refreshPlain = GenerateRefreshTokenValue();
            var refreshExpiry = DateTime.UtcNow.AddDays(Math.Max(1, _jwtSettings.RefreshTokenDays));
            await _refreshTokens.SaveAsync(memberId, refreshPlain, refreshExpiry);
            SetRefreshTokenCookie(refreshPlain, refreshExpiry);

            await _auditLog.WriteAsync(memberId, AuditAuthActions.LoginSuccess, IpHelper.GetClientIp(HttpContext), GetUserAgent(), true, null);

            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                token = jwt,
                memberId,
                email = login.UserName
            }, "Login successful"));
        }

        /// <summary>POST /api/Auth/refresh — reads refresh token from httpOnly cookie; returns new JWT and rotates refresh cookie.</summary>
        [HttpPost("refresh")]
        [AllowAnonymous]
        [EnableRateLimiting("refresh")]
        public async Task<IActionResult> Refresh()
        {
            if (!Request.Cookies.TryGetValue(RefreshTokenCookieName, out var oldToken) || string.IsNullOrWhiteSpace(oldToken))
                return Unauthorized(ApiResponse<object?>.ErrorResponse("No refresh session. Please sign in again."));

            var memberId = await _refreshTokens.TryValidateMemberIdAsync(oldToken);
            if (!memberId.HasValue)
            {
                ClearRefreshTokenCookie();
                return Unauthorized(ApiResponse<object?>.ErrorResponse("Session expired. Please sign in again."));
            }

            await _refreshTokens.RevokeAsync(oldToken);

            var email = await _refreshTokens.GetEmailByMemberIdAsync(memberId.Value);
            if (string.IsNullOrEmpty(email))
            {
                ClearRefreshTokenCookie();
                return Unauthorized(ApiResponse<object?>.ErrorResponse("Account not found."));
            }

            var expiryResult = await _userService.GetUserExpiryAsync(email);
            if (!expiryResult.IsSuccess || !expiryResult.Data.HasValue || expiryResult.Data.Value < DateTime.UtcNow)
            {
                ClearRefreshTokenCookie();
                return Unauthorized(ApiResponse<object?>.ErrorResponse("Account has expired"));
            }

            var newRefresh = GenerateRefreshTokenValue();
            var refreshExpiry = DateTime.UtcNow.AddDays(Math.Max(1, _jwtSettings.RefreshTokenDays));
            await _refreshTokens.SaveAsync(memberId.Value, newRefresh, refreshExpiry);
            SetRefreshTokenCookie(newRefresh, refreshExpiry);

            var jwt = CreateAccessToken(memberId.Value, email);

            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                token = jwt,
                memberId = memberId.Value,
                email
            }, "Token refreshed"));
        }

        /// <summary>POST /api/Auth/revoke — revokes refresh token cookie (logout).</summary>
        [HttpPost("revoke")]
        [AllowAnonymous]
        public async Task<IActionResult> Revoke()
        {
            long? memberId = null;
            if (Request.Cookies.TryGetValue(RefreshTokenCookieName, out var token) && !string.IsNullOrWhiteSpace(token))
            {
                memberId = await _refreshTokens.TryValidateMemberIdAsync(token);
                await _refreshTokens.RevokeAsync(token);
            }

            ClearRefreshTokenCookie();
            await _auditLog.WriteAsync(memberId, AuditAuthActions.Logout, IpHelper.GetClientIp(HttpContext), GetUserAgent(), true, null);

            return Ok(ApiResponse<object>.SuccessResponse(new { }, "Signed out"));
        }

        /// <summary>POST /api/Auth/register — creates account; sends verification email (no JWT).</summary>
        [HttpPost("register")]
        [AllowAnonymous]
        [EnableRateLimiting("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object?>.ErrorResponse("Validation failed", errors));
            }

            var result = await _userService.RegisterAsync(
                request.MemberName,
                request.UserName,
                request.EmailAddress,
                request.Mobile,
                request.Password,
                request.EmailAddress);

            if (!result.IsSuccess)
            {
                await _auditLog.WriteAsync(null, AuditAuthActions.Register, IpHelper.GetClientIp(HttpContext), GetUserAgent(), false, result.ErrorMessage);
                return BadRequest(ApiResponse<object?>.ErrorResponse(
                    "Registration failed", result.ErrorMessage!));
            }

            await _auditLog.WriteAsync(result.Data, AuditAuthActions.Register, IpHelper.GetClientIp(HttpContext), GetUserAgent(), true, null);

            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                memberId = result.Data,
                email = request.EmailAddress
            }, "Registration successful. Please check your email to verify your account."));
        }

        /// <summary>POST /api/Auth/verify-email — confirm email from link token.</summary>
        [HttpPost("verify-email")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object?>.ErrorResponse("Validation failed", errors));
            }

            var result = await _userService.VerifyEmailAsync(request.Token);
            if (!result.IsSuccess)
            {
                await _auditLog.WriteAsync(null, AuditAuthActions.EmailVerifyFailed, IpHelper.GetClientIp(HttpContext), GetUserAgent(), false, result.ErrorMessage);
                return BadRequest(ApiResponse<object?>.ErrorResponse(result.ErrorMessage ?? "Invalid or expired link"));
            }

            await _auditLog.WriteAsync(result.Data, AuditAuthActions.EmailVerified, IpHelper.GetClientIp(HttpContext), GetUserAgent(), true, null);

            return Ok(ApiResponse<object?>.SuccessResponse(null, "Email verified successfully"));
        }

        /// <summary>POST /api/Auth/resend-verification — new link if email exists and not verified (always generic success).</summary>
        [HttpPost("resend-verification")]
        [AllowAnonymous]
        [EnableRateLimiting("forgotpw")]
        public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object?>.ErrorResponse("Validation failed", errors));
            }

            await _userService.ResendVerificationAsync(request.Email);

            return Ok(ApiResponse<object>.SuccessResponse(
                new { },
                "If your email is registered, a new verification link has been sent."));
        }

        /// <summary>POST /api/Auth/forgot-password — always returns success; email sent only if account exists.</summary>
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        [EnableRateLimiting("forgotpw")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object?>.ErrorResponse("Validation failed", errors));
            }

            var memberId = await _userService.ForgotPasswordAsync(request.Email);
            await _auditLog.WriteAsync(memberId, AuditAuthActions.ForgotPassword, IpHelper.GetClientIp(HttpContext), GetUserAgent(), true, null);

            return Ok(ApiResponse<object>.SuccessResponse(
                new { },
                "If your email is registered, a reset link has been sent."));
        }

        /// <summary>POST /api/Auth/reset-password — set new password from email token; revokes all refresh sessions.</summary>
        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object?>.ErrorResponse("Validation failed", errors));
            }

            var result = await _userService.ResetPasswordAsync(request.Token, request.NewPassword);
            if (!result.IsSuccess)
            {
                await _auditLog.WriteAsync(null, AuditAuthActions.PasswordResetFailed, IpHelper.GetClientIp(HttpContext), GetUserAgent(), false, result.ErrorMessage);
                return BadRequest(ApiResponse<object?>.ErrorResponse(result.ErrorMessage ?? "Reset failed"));
            }

            await _refreshTokens.RevokeAllForMemberAsync(result.Data);
            ClearRefreshTokenCookie();

            await _auditLog.WriteAsync(result.Data, AuditAuthActions.PasswordReset, IpHelper.GetClientIp(HttpContext), GetUserAgent(), true, null);

            return Ok(ApiResponse<object>.SuccessResponse(new { }, "Password updated successfully. Please sign in."));
        }

        private string CreateAccessToken(long memberId, string email)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSettings.Key);
            var claims = new[]
            {
                new Claim(ClaimTypes.Email, email),
                new Claim("MemberId", memberId.ToString())
            };
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            return tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));
        }

        private static string GenerateRefreshTokenValue()
        {
            var bytes = new byte[64];
            RandomNumberGenerator.Fill(bytes);
            return WebEncoders.Base64UrlEncode(bytes);
        }

        private void SetRefreshTokenCookie(string token, DateTime expiresAtUtc)
        {
            var opts = new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                Expires = new DateTimeOffset(DateTime.SpecifyKind(expiresAtUtc, DateTimeKind.Utc))
            };
            Response.Cookies.Append(RefreshTokenCookieName, token, opts);
        }

        private void ClearRefreshTokenCookie()
        {
            Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions
            {
                Path = "/",
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax
            });
        }

        private string? GetUserAgent() => Request.Headers.UserAgent.ToString();
    }
}
