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
        public int ExpiryMinutes { get; set; } = 60;
    }
}
