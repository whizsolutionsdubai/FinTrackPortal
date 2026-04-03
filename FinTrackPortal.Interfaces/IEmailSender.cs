namespace FinTrackPortal.Interfaces
{
    /// <summary>Sends transactional email (verification, password reset).</summary>
    public interface IEmailSender
    {
        Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default);
    }
}
