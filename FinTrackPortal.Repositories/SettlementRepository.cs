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
    /// Dapper implementation of <see cref="ISettlementRepository"/>.
    /// All queries go through SQL Server stored procedures — no inline SQL.
    /// </summary>
    public class SettlementRepository : ISettlementRepository
    {
        private readonly IConfiguration _config;
        private readonly ILogger<SettlementRepository> _logger;

        public SettlementRepository(IConfiguration config, ILogger<SettlementRepository> logger)
        {
            _config = config;
            _logger = logger;
        }

        private IDbConnection Connection =>
            new SqlConnection(_config.GetConnectionString("DefaultConnection"));

        public async Task<OperationResult<long>> RecordSettlementAsync(
            long groupId, long fromMemberId, long toMemberId,
            decimal amount, string createdBy)
        {
            try
            {
                using var conn = Connection;
                var settlementId = await conn.QuerySingleAsync<long>(
                    "sp_RecordSettlement",
                    new
                    {
                        GroupId = groupId,
                        FromMemberId = fromMemberId,
                        ToMemberId = toMemberId,
                        Amount = amount,
                        CreatedBy = createdBy
                    },
                    commandType: CommandType.StoredProcedure);

                return OperationResult<long>.Success(settlementId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording settlement for group {GroupId}", groupId);
                return OperationResult<long>.Failure(ex.Message);
            }
        }

        public async Task<OperationResult<List<SettlementResponse>>> GetSettlementsByGroupAsync(long groupId)
        {
            try
            {
                using var conn = Connection;
                var settlements = (await conn.QueryAsync<SettlementResponse>(
                    "sp_GetSettlementsByGroup",
                    new { GroupId = groupId },
                    commandType: CommandType.StoredProcedure)).ToList();

                return OperationResult<List<SettlementResponse>>.Success(settlements);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching settlements for group {GroupId}", groupId);
                return OperationResult<List<SettlementResponse>>.Failure(ex.Message);
            }
        }
    }
}
