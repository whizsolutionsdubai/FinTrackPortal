using Dapper;
using FinTrackPortal.Interfaces;
using FinTrackPortal.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;

namespace FinTrackPortal.Repositories;

public sealed class BankDetailsRepository : IBankDetailsRepository
{
    private readonly IConfiguration _config;
    private readonly ILogger<BankDetailsRepository> _logger;

    public BankDetailsRepository(IConfiguration config, ILogger<BankDetailsRepository> logger)
    {
        _config = config;
        _logger = logger;
    }

    private IDbConnection Connection => new SqlConnection(_config.GetConnectionString("DefaultConnection"));

    public async Task<BankDetailsResponse?> GetForMemberAsync(long memberId)
    {
        try
        {
            using var conn = Connection;
            return await conn.QueryFirstOrDefaultAsync<BankDetailsResponse>(
                "sp_GetBankDetails",
                new { MemberId = memberId },
                commandType: CommandType.StoredProcedure);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetForMemberAsync bank details");
            return null;
        }
    }

    public async Task SaveAsync(long memberId, string bankName, string accountHolderName, string encryptedIban, string maskedIban)
    {
        using var conn = Connection;
        await conn.ExecuteAsync(
            "sp_SaveBankDetails",
            new
            {
                MemberId = memberId,
                BankName = bankName,
                AccountHolderName = accountHolderName,
                EncryptedIBAN = encryptedIban,
                MaskedIBAN = maskedIban
            },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<int> DeleteForMemberAsync(long memberId)
    {
        try
        {
            using var conn = Connection;
            var n = await conn.QueryFirstOrDefaultAsync<int?>(
                "sp_DeleteBankDetails",
                new { MemberId = memberId },
                commandType: CommandType.StoredProcedure);
            return n ?? 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteForMemberAsync");
            return 0;
        }
    }

    public async Task<PayeeBankEncryptedRow?> GetPayeeBankForSettlementAsync(decimal settlementId, long requestingMemberId)
    {
        try
        {
            using var conn = Connection;
            return await conn.QueryFirstOrDefaultAsync<PayeeBankEncryptedRow>(
                "sp_GetMemberBankDetailsForSettlement",
                new { SettlementId = settlementId, RequestingMemberId = requestingMemberId },
                commandType: CommandType.StoredProcedure);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetPayeeBankForSettlementAsync");
            return null;
        }
    }
}
