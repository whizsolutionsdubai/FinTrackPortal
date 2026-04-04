using System.Data;
using Dapper;
using FinTrackPortal.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FinTrackPortal.Repositories;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IConfiguration _config;
    private readonly ILogger<RefreshTokenRepository> _logger;

    public RefreshTokenRepository(IConfiguration config, ILogger<RefreshTokenRepository> logger)
    {
        _config = config;
        _logger = logger;
    }

    private IDbConnection Connection => new SqlConnection(_config.GetConnectionString("DefaultConnection"));

    public async Task SaveAsync(long memberId, string token, DateTime expiresAtUtc)
    {
        using var conn = Connection;
        await conn.ExecuteAsync(
            "sp_SaveRefreshToken",
            new { MemberId = memberId, Token = token, ExpiresAt = expiresAtUtc },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<long?> TryValidateMemberIdAsync(string token)
    {
        try
        {
            using var conn = Connection;
            return await conn.QuerySingleOrDefaultAsync<long?>(
                "sp_ValidateRefreshToken",
                new { Token = token },
                commandType: CommandType.StoredProcedure);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Validate refresh token failed");
            return null;
        }
    }

    public async Task RevokeAsync(string token)
    {
        using var conn = Connection;
        await conn.ExecuteAsync(
            "sp_RevokeRefreshToken",
            new { Token = token },
            commandType: CommandType.StoredProcedure);
    }

    public async Task RevokeAllForMemberAsync(long memberId)
    {
        using var conn = Connection;
        await conn.ExecuteAsync(
            "sp_RevokeAllUserRefreshTokens",
            new { MemberId = memberId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<string?> GetEmailByMemberIdAsync(long memberId)
    {
        using var conn = Connection;
        return await conn.ExecuteScalarAsync<string?>(
            "sp_GetUserEmailByMemberId",
            new { MemberId = memberId },
            commandType: CommandType.StoredProcedure);
    }
}
