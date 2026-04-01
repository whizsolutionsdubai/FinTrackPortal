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

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest login)
        {
            // 1. Validate credentials — returns MemberId on success
            var validationResult = await _userService.ValidateUserAsync(login.UserName, login.Password);
            if (!validationResult.IsSuccess)
                return Unauthorized("Invalid credentials");

            var memberId = validationResult.Data;

            // 2. Get user expiry
            var expiryResult = await _userService.GetUserExpiryAsync(login.UserName);
            if (!expiryResult.IsSuccess || !expiryResult.Data.HasValue)
                return Unauthorized("Unable to retrieve account expiry");

            if (expiryResult.Data.Value < DateTime.UtcNow)
                return Unauthorized("Account has expired");

            // 3. Generate JWT with Email + MemberId claims
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
            return Ok(new { token = tokenHandler.WriteToken(token) });
        }
    }
}
