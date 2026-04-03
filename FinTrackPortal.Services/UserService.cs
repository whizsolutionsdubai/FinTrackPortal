using FinTrackPortal.Common;
using FinTrackPortal.Interfaces;
using FinTrackPortal.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FinTrackPortal.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repository;
        private readonly IEmailSender _emailSender;
        private readonly AppEmailOptions _emailSettings;
        private readonly ILogger<UserService> _logger;

        public UserService(
            IUserRepository repository,
            IEmailSender emailSender,
            IOptions<AppEmailOptions> emailOptions,
            ILogger<UserService> logger)
        {
            _repository = repository;
            _emailSender = emailSender;
            _emailSettings = emailOptions.Value;
            _logger = logger;
        }

        public Task<OperationResult<long>> ValidateUserAsync(string username, string password)
            => _repository.ValidateUserAsync(username, password);

        public Task<OperationResult<DateTime?>> GetUserExpiryAsync(string username)
            => _repository.GetUserExpiryAsync(username);

        public async Task<OperationResult<long>> RegisterAsync(
            string memberName, string userName, string emailAddress,
            string? mobile, string password, string createdBy)
        {
            var (ok, msg) = PasswordValidator.Validate(password);
            if (!ok)
                return OperationResult<long>.Failure(msg);

            var result = await _repository.RegisterAsync(memberName, userName, emailAddress, mobile, password, createdBy);
            if (!result.IsSuccess)
                return result;

            var verifyToken = Guid.NewGuid().ToString("N");
            try
            {
                await _repository.SaveEmailVerifyTokenAsync(emailAddress, verifyToken, 24);
                var link = $"{_emailSettings.AppPublicUrl.TrimEnd('/')}/verify-email?token={verifyToken}";
                var body =
                    $"<p>Hi {System.Net.WebUtility.HtmlEncode(memberName)},</p>" +
                    "<p>Welcome to FinShare! Please click the link below to verify your email address:</p>" +
                    $"<p><a href=\"{System.Net.WebUtility.HtmlEncode(link)}\">Verify your email</a></p>" +
                    "<p>This link expires in 24 hours.</p>" +
                    "<p>If you did not create a FinShare account, please ignore this email.</p>" +
                    "<p>— The FinShare Team</p>";

                await _emailSender.SendAsync(
                    emailAddress,
                    "Verify your FinShare email address",
                    body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send verification email to {Email}", emailAddress);
            }

            return result;
        }

        public Task<OperationResult<bool>> VerifyEmailAsync(string token)
            => _repository.VerifyEmailWithTokenAsync(token);

        public async Task ForgotPasswordAsync(string email)
        {
            try
            {
                var memberName = await _repository.GetMemberNameByEmailAsync(email);
                if (string.IsNullOrEmpty(memberName))
                    return;

                var resetToken = Guid.NewGuid().ToString("N");
                await _repository.SavePasswordResetTokenAsync(email, resetToken, DateTime.UtcNow.AddHours(1));

                var link = $"{_emailSettings.AppPublicUrl.TrimEnd('/')}/reset-password?token={resetToken}";
                var body =
                    $"<p>Hi {System.Net.WebUtility.HtmlEncode(memberName)},</p>" +
                    "<p>We received a request to reset your FinShare password.</p>" +
                    "<p>Click the link below to set a new password. This link expires in 1 hour.</p>" +
                    $"<p><a href=\"{System.Net.WebUtility.HtmlEncode(link)}\">Reset your password</a></p>" +
                    "<p>If you did not request this, you can safely ignore this email. Your password will not change.</p>" +
                    "<p>— The FinShare Team</p>";

                await _emailSender.SendAsync(email, "FinShare — Reset your password", body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Forgot-password flow failed for {Email}", email);
            }
        }

        public async Task<OperationResult<bool>> ResetPasswordAsync(string token, string newPassword)
        {
            var (ok, msg) = PasswordValidator.Validate(newPassword);
            if (!ok)
                return OperationResult<bool>.Failure(msg);

            var updated = await _repository.ResetPasswordWithTokenAsync(token, newPassword);
            if (!updated)
                return OperationResult<bool>.Failure("This reset link has expired or is invalid. Please request a new one.");

            return OperationResult<bool>.Success(true);
        }
    }
}
