using Azure.Identity;
using FinTrackPortal.Interfaces;
using FinTrackPortal.Models;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace FinTrackPortal.API.Services
{
    /// <summary>Sends transactional email via Microsoft Graph (Microsoft 365) using client-credentials OAuth2.</summary>
    public sealed class MicrosoftGraphEmailSender : IEmailSender
    {
        private readonly AppEmailOptions _settings;
        private readonly ILogger<MicrosoftGraphEmailSender> _logger;
        private readonly object _lock = new();
        private GraphServiceClient? _graphClient;

        public MicrosoftGraphEmailSender(IOptions<AppEmailOptions> options, ILogger<MicrosoftGraphEmailSender> logger)
        {
            _settings = options.Value;
            _logger = logger;
        }

        public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
        {
            var g = _settings.Graph;
            if (string.IsNullOrWhiteSpace(g.TenantId)
                || string.IsNullOrWhiteSpace(g.ClientId)
                || string.IsNullOrWhiteSpace(g.ClientSecret)
                || string.IsNullOrWhiteSpace(g.SenderMailbox))
            {
                _logger.LogWarning(
                    "Email skipped (Graph TenantId, ClientId, ClientSecret, or SenderMailbox missing). To={To}",
                    toEmail);
                return;
            }

            try
            {
                var client = GetOrCreateClient(g);

                var body = new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
                {
                    Message = new Message
                    {
                        Subject = subject,
                        Body = new ItemBody { ContentType = BodyType.Html, Content = htmlBody },
                        ToRecipients = new List<Recipient>
                        {
                            new()
                            {
                                EmailAddress = new EmailAddress { Address = toEmail }
                            }
                        }
                    },
                    SaveToSentItems = false
                };

                await client.Users[g.SenderMailbox].SendMail.PostAsync(body, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Microsoft Graph send failed for {To}", toEmail);
                throw;
            }
        }

        private GraphServiceClient GetOrCreateClient(GraphEmailOptions g)
        {
            lock (_lock)
            {
                if (_graphClient != null)
                    return _graphClient;

                var credential = new ClientSecretCredential(g.TenantId, g.ClientId, g.ClientSecret);
                var scopes = new[] { "https://graph.microsoft.com/.default" };
                _graphClient = new GraphServiceClient(credential, scopes);
                return _graphClient;
            }
        }
    }
}
