using Dapper;
using FinTrackPortal.Interfaces;
using FinTrackPortal.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;

namespace FinTrackPortal.Repositories;

public sealed class TransactionRepository : ITransactionRepository
{
    private readonly IConfiguration _config;
    private readonly ILogger<TransactionRepository> _logger;

    public TransactionRepository(IConfiguration config, ILogger<TransactionRepository> logger)
    {
        _config = config;
        _logger = logger;
    }

    private IDbConnection Connection => new SqlConnection(_config.GetConnectionString("DefaultConnection"));

    public async Task<IReadOnlyList<TransactionHistoryItem>> GetHistoryAsync(long memberId, int skip, int take)
    {
        try
        {
            using var conn = Connection;
            var rows = await conn.QueryAsync<TransactionHistoryItem>(
                "sp_GetMemberTransactionHistory",
                new { MemberId = memberId, Skip = skip, Take = take },
                commandType: CommandType.StoredProcedure);
            return rows.AsList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetHistoryAsync for member {MemberId}", memberId);
            return Array.Empty<TransactionHistoryItem>();
        }
    }

    public async Task<TransactionSummaryResponse?> GetSummaryAsync(long memberId)
    {
        try
        {
            using var conn = Connection;
            return await conn.QueryFirstOrDefaultAsync<TransactionSummaryResponse>(
                "sp_GetMemberTransactionSummary",
                new { MemberId = memberId },
                commandType: CommandType.StoredProcedure);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetSummaryAsync for member {MemberId}", memberId);
            return null;
        }
    }
}
