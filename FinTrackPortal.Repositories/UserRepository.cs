using Dapper;
using FinTrackPortal.Common;
using FinTrackPortal.Interfaces;
using FinTrackPortal.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace FinTrackPortal.Repositories
{
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

                var memberId = await conn.QueryFirstOrDefaultAsync<long?>(
                    "sp_ValidateUser",
                    new { UserName = username, PasswordHash = HashPassword(password) },
                    commandType: CommandType.StoredProcedure);

                if (memberId == null)
                    return OperationResult<long>.Failure("User not found or invalid credentials.");

                return OperationResult<long>.Success(memberId.Value);
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

                // Use nullable DateTime to handle NULL from DB
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

        private static string HashPassword(string password)
        {
            return password;
            // hided code due to the password encryption stopped for no and will activate in the production
            //using var sha = SHA256.Create();
            //var bytes = Encoding.UTF8.GetBytes(password);
            //var hash = sha.ComputeHash(bytes);
            //return Convert.ToBase64String(hash);
        }

    }
}
