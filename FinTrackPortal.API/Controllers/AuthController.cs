using FinTrackPortal.Common;
using FinTrackPortal.Interfaces;
using FinTrackPortal.Models;
using FinTrackPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FinTrackPortal.API.Controllers
{
    /// <summary>
    /// Authentication — login, registration, email verification, forgot/reset password.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly JwtSettings _jwtSettings;
        private readonly IUserService _userService;
        private readonly IAuditLogRepository _auditLog;

        public AuthController(
            IOptions<JwtSettings> jwtOptions,
            IUserService userService,
            IAuditLogRepository auditLog)
        {
            _jwtSettings = jwtOptions.Value;
            _userService = userService;
            _auditLog = auditLog;
        }

        /// <summary>POST /api/Auth/login — JWT after email verification and valid credentials.</summary>
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
                    GetClientIp(),
                    GetUserAgent(),
                    false,
                    locked ? validationResult.ErrorMessage : "Invalid credentials");
                return Unauthorized(ApiResponse<object?>.ErrorResponse(validationResult.ErrorMessage ?? "Invalid credentials"));
            }

            var memberId = validationResult.Data;

            var expiryResult = await _userService.GetUserExpiryAsync(login.UserName);
            if (!expiryResult.IsSuccess || !expiryResult.Data.HasValue)
            {
                await _auditLog.WriteAsync(memberId, AuditAuthActions.LoginFailed, GetClientIp(), GetUserAgent(), false, "Unable to retrieve account expiry");
                return Unauthorized(ApiResponse<object?>.ErrorResponse("Unable to retrieve account expiry"));
            }

            if (expiryResult.Data.Value < DateTime.UtcNow)
            {
                await _auditLog.WriteAsync(memberId, AuditAuthActions.LoginFailed, GetClientIp(), GetUserAgent(), false, "Account expired");
                return Unauthorized(ApiResponse<object?>.ErrorResponse("Account has expired"));
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSettings.Key);
            var claims = new[]
            {
                new Claim(ClaimTypes.Email, login.UserName),
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

            var token = tokenHandler.CreateToken(tokenDescriptor);

            await _auditLog.WriteAsync(memberId, AuditAuthActions.LoginSuccess, GetClientIp(), GetUserAgent(), true, null);

            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                token = tokenHandler.WriteToken(token),
                memberId,
                email = login.UserName
            }, "Login successful"));
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
                await _auditLog.WriteAsync(null, AuditAuthActions.Register, GetClientIp(), GetUserAgent(), false, result.ErrorMessage);
                return BadRequest(ApiResponse<object?>.ErrorResponse(
                    "Registration failed", result.ErrorMessage!));
            }

            await _auditLog.WriteAsync(result.Data, AuditAuthActions.Register, GetClientIp(), GetUserAgent(), true, null);

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
                await _auditLog.WriteAsync(null, AuditAuthActions.EmailVerifyFailed, GetClientIp(), GetUserAgent(), false, result.ErrorMessage);
                return BadRequest(ApiResponse<object?>.ErrorResponse(result.ErrorMessage ?? "Invalid or expired link"));
            }

            await _auditLog.WriteAsync(result.Data, AuditAuthActions.EmailVerified, GetClientIp(), GetUserAgent(), true, null);

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
            await _auditLog.WriteAsync(memberId, AuditAuthActions.ForgotPassword, GetClientIp(), GetUserAgent(), true, null);

            return Ok(ApiResponse<object>.SuccessResponse(
                new { },
                "If your email is registered, a reset link has been sent."));
        }

        /// <summary>POST /api/Auth/reset-password — set new password from email token.</summary>
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
                await _auditLog.WriteAsync(null, AuditAuthActions.PasswordResetFailed, GetClientIp(), GetUserAgent(), false, result.ErrorMessage);
                return BadRequest(ApiResponse<object?>.ErrorResponse(result.ErrorMessage ?? "Reset failed"));
            }

            await _auditLog.WriteAsync(result.Data, AuditAuthActions.PasswordReset, GetClientIp(), GetUserAgent(), true, null);

            return Ok(ApiResponse<object>.SuccessResponse(new { }, "Password updated successfully. Please sign in."));
        }

        private string GetClientIp()
        {
            var forwarded = Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwarded))
                return forwarded.Split(',')[0].Trim();
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        private string? GetUserAgent() => Request.Headers.UserAgent.ToString();
    }
}
