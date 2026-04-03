namespace FinTrackPortal.Common
{
    /// <summary>Server-side password rules for register and reset-password (FinShare auth spec).</summary>
    public static class PasswordValidator
    {
        private const string AllowedSpecial = "!@#$%^&*-_=+";

        public static (bool Valid, string Message) Validate(string? password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 8)
                return (false, "Password must be at least 8 characters.");

            if (password.Length > 64)
                return (false, "Password must not exceed 64 characters.");

            if (password.Contains(' ', StringComparison.Ordinal))
                return (false, "Password must not contain spaces.");

            if (!password.Any(char.IsUpper))
                return (false, "Password must contain at least one uppercase letter.");

            if (!password.Any(char.IsLower))
                return (false, "Password must contain at least one lowercase letter.");

            if (!password.Any(char.IsDigit))
                return (false, "Password must contain at least one number.");

            if (!password.Any(c => AllowedSpecial.Contains(c)))
                return (false, $"Password must contain at least one special character ({AllowedSpecial}).");

            return (true, "Password is strong.");
        }
    }
}
