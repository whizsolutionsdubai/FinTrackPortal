namespace FinTrackPortal.Models
{
    /// <summary>Outbound email (configuration section <c>Email</c>). Microsoft 365 uses <see cref="Graph"/>.</summary>
    public class AppEmailOptions
    {
        public const string SectionName = "Email";

        public bool Enabled { get; set; }

        /// <summary><c>MicrosoftGraph</c> (Microsoft 365 / Graph API) or <c>Smtp</c>.</summary>
        public string Provider { get; set; } = "MicrosoftGraph";

        public string FromName { get; set; } = "FinShare";

        /// <summary>SMTP From address. For Graph, the sending mailbox is <see cref="GraphEmailOptions.SenderMailbox"/>.</summary>
        public string FromEmail { get; set; } = string.Empty;

        /// <summary>Public site base URL for verification and reset links (no trailing slash).</summary>
        public string AppPublicUrl { get; set; } = "https://finshare.me";

        /// <summary>Required when <see cref="Provider"/> is <c>MicrosoftGraph</c> (Entra app + <c>Mail.Send</c> application permission).</summary>
        public GraphEmailOptions Graph { get; set; } = new();

        // ---- SMTP only (Provider = Smtp) ----

        public string SmtpHost { get; set; } = string.Empty;

        public int SmtpPort { get; set; } = 587;

        public bool UseSsl { get; set; } = true;

        public string? SmtpUser { get; set; }

        public string? SmtpPassword { get; set; }
    }

    /// <summary>Azure AD app registration (client credentials) for Microsoft Graph.</summary>
    public class GraphEmailOptions
    {
        public string TenantId { get; set; } = string.Empty;

        public string ClientId { get; set; } = string.Empty;

        public string ClientSecret { get; set; } = string.Empty;

        /// <summary>
        /// Licensed mailbox that sends mail (UPN or object id), e.g. <c>noreply@contoso.onmicrosoft.com</c>.
        /// Application permission <c>Mail.Send</c> with admin consent.
        /// </summary>
        public string SenderMailbox { get; set; } = string.Empty;
    }
}
