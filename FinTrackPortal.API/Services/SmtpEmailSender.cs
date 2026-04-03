using FinTrackPortal.Interfaces;
using FinTrackPortal.Models;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace FinTrackPortal.API.Services
{
    /// <summary>Sends email via SMTP when <see cref="AppEmailOptions"/> is enabled and host is set.</summary>
    public class SmtpEmailSender : IEmailSender
    {
        private readonly AppEmailOptions _settings;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(IOptions<AppEmailOptions> options, ILogger<SmtpEmailSender> logger)
        {
            _settings = options.Value;
            _logger = logger;
        }

        public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
        {
            if (!_settings.Enabled
                || string.IsNullOrWhiteSpace(_settings.SmtpHost)
                || string.IsNullOrWhiteSpace(_settings.FromEmail))
            {
                _logger.LogWarning(
                    "Email not sent (Email:Enabled=false or SmtpHost/FromEmail missing). To={To}, Subject={Subject}",
                    toEmail,
                    subject);
                return;
            }

            using var message = new MailMessage
            {
                From = new MailAddress(_settings.FromEmail, _settings.FromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
            {
                EnableSsl = _settings.UseSsl
            };

            if (!string.IsNullOrEmpty(_settings.SmtpUser))
                client.Credentials = new NetworkCredential(_settings.SmtpUser, _settings.SmtpPassword);

            await client.SendMailAsync(message, cancellationToken);
        }
    }
}
