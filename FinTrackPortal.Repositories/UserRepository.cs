using Dapper;
using FinTrackPortal.Common;
using FinTrackPortal.Interfaces;
using FinTrackPortal.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;

namespace FinTrackPortal.Repositories
{
    /// <summary>
    /// Dapper implementation of <see cref="IUserRepository"/>.
    /// All queries go through SQL Server stored procedures — no inline SQL.
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly IConfiguration _config;

        private readonly ILogger<UserRepository> _logger;

        public UserRepository(IConfiguration config, ILogger<UserRepository> logger)
        {
            _config = config;
            _logger = logger;
        }

        private IDbConnection Connection => new SqlConnection(_config.GetConnectionString("DefaultConnection"));

        public async Task<OperationResult<long>> ValidateUserAsync(string username, string password)
        {
            try
            {
                using var conn = Connection;

                var row = await conn.QueryFirstOrDefaultAsync<ValidateUserRow>(
                    "sp_ValidateUser",
                    new { UserName = username },
                    commandType: CommandType.StoredProcedure);

                if (row == null || string.IsNullOrEmpty(row.PasswordHash))
                    return OperationResult<long>.Failure("User not found or invalid credentials.");

                if (row.LockoutUntil.HasValue && row.LockoutUntil.Value > DateTime.UtcNow)
                    return OperationResult<long>.Failure("Account is temporarily locked due to too many failed attempts. Please try again later.");

                if (!BCrypt.Net.BCrypt.Verify(password, row.PasswordHash))
                {
                    await conn.ExecuteAsync(
                        "sp_RecordFailedLogin",
                        new { UserName = username },
                        commandType: CommandType.StoredProcedure);
                    return OperationResult<long>.Failure("User not found or invalid credentials.");
                }

                if (!row.IsEmailVerified)
                    return OperationResult<long>.Failure("Please verify your email before logging in.");

                await conn.ExecuteAsync(
                    "sp_ClearFailedLogins",
                    new { UserName = username },
                    commandType: CommandType.StoredProcedure);

                return OperationResult<long>.Success(row.MemberId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating user {Username}", username);
                return OperationResult<long>.Failure(ex.Message);
            }
        }

        public async Task<OperationResult<DateTime?>> GetUserExpiryAsync(string username)
        {
            try
            {
                using var conn = Connection;

                var expiryDate = await conn.QueryFirstOrDefaultAsync<DateTime?>("sp_GetExpiry", new { UserName = username }, commandType: CommandType.StoredProcedure);

                if (expiryDate == null)
                    return OperationResult<DateTime?>.Failure("User not found or no expiry date set.");

                return OperationResult<DateTime?>.Success(expiryDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching expiryDate for username {username}", username);
                return OperationResult<DateTime?>.Failure(ex.Message);
            }
        }

        public async Task<OperationResult<long>> RegisterAsync(
            string memberName, string userName, string emailAddress,
            string? mobile, string password, string createdBy)
        {
            try
            {
                using var conn = Connection;
                var memberId = await conn.QuerySingleAsync<long>(
                    "sp_RegisterUser",
                    new
                    {
                        MemberName = memberName,
                        UserName = userName,
                        EmailAddress = emailAddress,
                        Mobile = mobile,
                        PasswordHash = HashPassword(password),
                        CreatedBy = createdBy,
                        ExpiryDate = DateTime.UtcNow.AddYears(1)
                    },
                    commandType: CommandType.StoredProcedure);

                return OperationResult<long>.Success(memberId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering user {UserName}", userName);
                return OperationResult<long>.Failure(ex.Message);
            }
        }

        public async Task SaveEmailVerifyTokenAsync(string email, string token, int expiryHours)
        {
            using var conn = Connection;
            await conn.ExecuteAsync(
                "sp_SaveEmailVerifyToken",
                new { Email = email, Token = token, ExpiryHours = expiryHours },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<OperationResult<long>> VerifyEmailWithTokenAsync(string token)
        {
            try
            {
                using var conn = Connection;
                var row = await conn.QueryFirstOrDefaultAsync<VerifyEmailRow>(
                    "sp_VerifyEmail",
                    new { Token = token },
                    commandType: CommandType.StoredProcedure);

                if (row == null)
                    return OperationResult<long>.Failure("Invalid or expired link.");

                if (!row.Success)
                    return OperationResult<long>.Failure(row.Message ?? "Invalid or expired link.");

                if (!row.MemberId.HasValue)
                    return OperationResult<long>.Failure("Invalid or expired link.");

                return OperationResult<long>.Success(row.MemberId.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying email token");
                return OperationResult<long>.Failure(ex.Message);
            }
        }

        public async Task<MemberEmailLookup?> LookupMemberByEmailAsync(string email)
        {
            using var conn = Connection;
            return await conn.QueryFirstOrDefaultAsync<MemberEmailLookup>(
                "sp_GetMemberNameByEmail",
                new { Email = email },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<UserEmailVerificationStatus?> GetUserEmailVerificationStatusAsync(string email)
        {
            using var conn = Connection;
            return await conn.QueryFirstOrDefaultAsync<UserEmailVerificationStatus>(
                "sp_GetUserEmailVerificationStatus",
                new { Email = email },
                commandType: CommandType.StoredProcedure);
        }

        public async Task SavePasswordResetTokenAsync(string email, string token, DateTime expiryUtc)
        {
            using var conn = Connection;
            await conn.ExecuteAsync(
                "sp_SavePasswordResetToken",
                new { Email = email, ResetToken = token, ExpiryUTC = expiryUtc },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<OperationResult<long>> ResetPasswordWithTokenAsync(string token, string newPlainPassword)
        {
            using var conn = Connection;
            var row = await conn.QueryFirstOrDefaultAsync<ResetPasswordRow>(
                "sp_ResetPassword",
                new { Token = token, NewPasswordHash = HashPassword(newPlainPassword) },
                commandType: CommandType.StoredProcedure);

            if (row == null || row.RowsUpdated <= 0 || !row.MemberId.HasValue)
                return OperationResult<long>.Failure("This reset link has expired or is invalid. Please request a new one.");

            return OperationResult<long>.Success(row.MemberId.Value);
        }

        private static string HashPassword(string password)
            => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

        private sealed class ValidateUserRow
        {
            public long MemberId { get; set; }
            public string? PasswordHash { get; set; }
            public bool IsEmailVerified { get; set; }
            public DateTime? LockoutUntil { get; set; }
            public int FailedLoginCount { get; set; }
        }

        private sealed class VerifyEmailRow
        {
            public bool Success { get; set; }
            public string? Message { get; set; }
            public long? MemberId { get; set; }
        }

        private sealed class ResetPasswordRow
        {
            public int RowsUpdated { get; set; }
            public long? MemberId { get; set; }
        }
    }
}
