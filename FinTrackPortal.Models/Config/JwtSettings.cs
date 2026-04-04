namespace FinTrackPortal.Models
{
    /// <summary>
    /// Bound from the "JwtSettings" section of appsettings.json.
    /// Used in Program.cs to configure JWT Bearer authentication.
    /// </summary>
    public class JwtSettings
    {
        public string Key { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;

        /// <summary>Access token (JWT) lifetime in minutes.</summary>
        public int ExpiryMinutes { get; set; } = 60;

        /// <summary>Refresh token lifetime in days (httpOnly cookie).</summary>
        public int RefreshTokenDays { get; set; } = 14;
    }
}
