using FinTrackPortal.Interfaces;
using FinTrackPortal.Models;
using Microsoft.Extensions.Options;

namespace FinTrackPortal.API.Services
{
    /// <summary>Routes to Microsoft Graph or SMTP based on <see cref="AppEmailOptions.Provider"/>.</summary>
    public sealed class EmailSenderSelector : IEmailSender
    {
        private readonly AppEmailOptions _settings;
        private readonly MicrosoftGraphEmailSender _graph;
        private readonly SmtpEmailSender _smtp;
        private readonly ILogger<EmailSenderSelector> _logger;

        public EmailSenderSelector(
            IOptions<AppEmailOptions> options,
            MicrosoftGraphEmailSender graph,
            SmtpEmailSender smtp,
            ILogger<EmailSenderSelector> logger)
        {
            _settings = options.Value;
            _graph = graph;
            _smtp = smtp;
            _logger = logger;
        }

        public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
        {
            if (!_settings.Enabled)
            {
                _logger.LogWarning(
                    "Email not sent (Email:Enabled=false). To={To}, Subject={Subject}",
                    toEmail,
                    subject);
                return Task.CompletedTask;
            }

            if (string.Equals(_settings.Provider, "Smtp", StringComparison.OrdinalIgnoreCase))
                return _smtp.SendAsync(toEmail, subject, htmlBody, cancellationToken);

            return _graph.SendAsync(toEmail, subject, htmlBody, cancellationToken);
        }
    }
}
