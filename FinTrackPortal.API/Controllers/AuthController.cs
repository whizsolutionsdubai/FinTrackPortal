using FinTrackPortal.Common;
using FinTrackPortal.Models;
using FinTrackPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public AuthController(IOptions<JwtSettings> jwtOptions, IUserService userService)
        {
            _jwtSettings = jwtOptions.Value;
            _userService = userService;
        }

        /// <summary>POST /api/Auth/login — JWT after email verification and valid credentials.</summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest login)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object?>.ErrorResponse("Validation failed", errors));
            }

            var validationResult = await _userService.ValidateUserAsync(login.UserName, login.Password);
            if (!validationResult.IsSuccess)
                return Unauthorized(ApiResponse<object?>.ErrorResponse(validationResult.ErrorMessage ?? "Invalid credentials"));

            var memberId = validationResult.Data;

            var expiryResult = await _userService.GetUserExpiryAsync(login.UserName);
            if (!expiryResult.IsSuccess || !expiryResult.Data.HasValue)
                return Unauthorized(ApiResponse<object?>.ErrorResponse("Unable to retrieve account expiry"));

            if (expiryResult.Data.Value < DateTime.UtcNow)
                return Unauthorized(ApiResponse<object?>.ErrorResponse("Account has expired"));

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
                return BadRequest(ApiResponse<object?>.ErrorResponse(
                    "Registration failed", result.ErrorMessage!));

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
                return BadRequest(ApiResponse<object?>.ErrorResponse(result.ErrorMessage ?? "Invalid or expired link"));

            return Ok(ApiResponse<object?>.SuccessResponse(null, "Email verified successfully"));
        }

        /// <summary>POST /api/Auth/forgot-password — always returns success; email sent only if account exists.</summary>
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object?>.ErrorResponse("Validation failed", errors));
            }

            await _userService.ForgotPasswordAsync(request.Email);

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
                return BadRequest(ApiResponse<object?>.ErrorResponse(result.ErrorMessage ?? "Reset failed"));

            return Ok(ApiResponse<object>.SuccessResponse(new { }, "Password updated successfully. Please sign in."));
        }
    }
}
