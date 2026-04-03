using System.Data;
using Dapper;
using FinTrackPortal.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FinTrackPortal.Repositories;

public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly IConfiguration _config;
    private readonly ILogger<AuditLogRepository> _logger;

    public AuditLogRepository(IConfiguration config, ILogger<AuditLogRepository> logger)
    {
        _config = config;
        _logger = logger;
    }

    private IDbConnection Connection => new SqlConnection(_config.GetConnectionString("DefaultConnection"));

    public async Task WriteAsync(long? memberId, string action, string? ipAddress, string? userAgent, bool success, string? details)
    {
        try
        {
            using var conn = Connection;
            await conn.ExecuteAsync(
                "sp_WriteAuditLog",
                new
                {
                    MemberId = memberId,
                    Action = action,
                    IPAddress = ipAddress,
                    UserAgent = userAgent,
                    Success = success,
                    Details = details
                },
                commandType: CommandType.StoredProcedure);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Audit log write failed for action {Action}", action);
        }
    }
}
