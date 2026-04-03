using System.Data;
using Microsoft.Data.SqlClient;

namespace FinTrackPortal.API.Services;

/// <summary>Runs <c>sp_CleanupExpiredTokens</c> hourly and audit retention daily.</summary>
public sealed class SecurityMaintenanceHostedService : BackgroundService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SecurityMaintenanceHostedService> _logger;

    public SecurityMaintenanceHostedService(IConfiguration config, ILogger<SecurityMaintenanceHostedService> logger)
    {
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var cs = _config.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(cs))
        {
            _logger.LogWarning("SecurityMaintenanceHostedService: no DefaultConnection; skipping.");
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        var lastArchiveUtc = DateTime.MinValue;

        try
        {
            await RunTokenCleanupAsync(cs, stoppingToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Initial token cleanup failed.");
        }

        try
        {
            if ((DateTime.UtcNow - lastArchiveUtc).TotalHours >= 24)
            {
                await RunAuditArchiveAsync(cs, stoppingToken);
                lastArchiveUtc = DateTime.UtcNow;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Initial audit archive failed.");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            try
            {
                await RunTokenCleanupAsync(cs, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Token cleanup failed.");
            }

            if ((DateTime.UtcNow - lastArchiveUtc).TotalHours < 24)
                continue;

            try
            {
                await RunAuditArchiveAsync(cs, stoppingToken);
                lastArchiveUtc = DateTime.UtcNow;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Audit archive retention failed.");
            }
        }
    }

    private static async Task RunTokenCleanupAsync(string connectionString, CancellationToken ct)
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);
        await using var cmd = new SqlCommand("sp_CleanupExpiredTokens", conn) { CommandType = CommandType.StoredProcedure };
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static async Task RunAuditArchiveAsync(string connectionString, CancellationToken ct)
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);
        await using var cmd = new SqlCommand("sp_ArchiveAuditLogsRetention", conn) { CommandType = CommandType.StoredProcedure };
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
