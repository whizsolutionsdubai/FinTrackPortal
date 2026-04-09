using Dapper;
using FinTrackPortal.Interfaces;
using FinTrackPortal.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;

namespace FinTrackPortal.Repositories;

public sealed class GroupChecklistRepository : IGroupChecklistRepository
{
    private readonly IConfiguration _config;
    private readonly ILogger<GroupChecklistRepository> _logger;

    public GroupChecklistRepository(IConfiguration config, ILogger<GroupChecklistRepository> logger)
    {
        _config = config;
        _logger = logger;
    }

    private IDbConnection Connection => new SqlConnection(_config.GetConnectionString("DefaultConnection"));

    public async Task<IReadOnlyList<EventChecklistItemResponse>> GetChecklistAsync(long eventId, long requestingMemberId)
    {
        try
        {
            using var conn = Connection;
            var items = (await conn.QueryAsync<EventChecklistItemResponse>(
                "sp_GetEventChecklist",
                new { EventId = eventId, RequestingMemberId = requestingMemberId },
                commandType: CommandType.StoredProcedure)).AsList();

            var claims = (await conn.QueryAsync<ChecklistClaimRow>(
                "sp_GetEventChecklistClaims",
                new { EventId = eventId },
                commandType: CommandType.StoredProcedure)).AsList();

            var byItem = claims.GroupBy(c => c.ChecklistItemId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => new ChecklistClaimSummary
                    {
                        MemberId = x.MemberId,
                        MemberName = x.MemberName,
                        QuantityClaimed = x.QuantityClaimed,
                        ClaimedDate = x.ClaimedDate
                    }).ToList());

            foreach (var item in items)
                item.Claims = byItem.TryGetValue(item.ChecklistItemId, out var list) ? list : new List<ChecklistClaimSummary>();

            return items;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetChecklistAsync");
            return Array.Empty<EventChecklistItemResponse>();
        }
    }

    public async Task<long?> AddItemAsync(long eventId, AddChecklistItemRequest request)
    {
        try
        {
            using var conn = Connection;
            return await conn.QueryFirstOrDefaultAsync<long?>(
                "sp_AddChecklistItem",
                new
                {
                    EventId = eventId,
                    request.ItemName,
                    request.QuantityNeeded,
                    request.QuantityUnit,
                    request.SuggestedMemberId,
                    request.IsMandatory,
                    request.AssignedMemberId
                },
                commandType: CommandType.StoredProcedure);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AddItemAsync");
            return null;
        }
    }

    public async Task<(int Result, string? Message)> UpdateItemAsync(long checklistItemId, UpdateChecklistItemRequest request)
    {
        try
        {
            using var conn = Connection;
            var row = await conn.QueryFirstOrDefaultAsync<ChecklistResultRow>(
                "sp_UpdateChecklistItem",
                new
                {
                    ChecklistItemId = checklistItemId,
                    request.ItemName,
                    request.QuantityNeeded,
                    request.QuantityUnit,
                    request.SuggestedMemberId,
                    request.IsMandatory,
                    request.AssignedMemberId,
                    request.Status
                },
                commandType: CommandType.StoredProcedure);
            return (row?.Result ?? 0, row?.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateItemAsync");
            return (-1, ex.Message);
        }
    }

    public async Task<int> DeleteItemAsync(long checklistItemId)
    {
        try
        {
            using var conn = Connection;
            return await conn.ExecuteAsync(
                "sp_DeleteChecklistItem",
                new { ChecklistItemId = checklistItemId },
                commandType: CommandType.StoredProcedure);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteItemAsync");
            return 0;
        }
    }

    public async Task<(int Result, string? Message)> ClaimItemAsync(long checklistItemId, long memberId, int quantityClaimed)
    {
        try
        {
            using var conn = Connection;
            var row = await conn.QueryFirstOrDefaultAsync<ChecklistResultRow>(
                "sp_ClaimChecklistItem",
                new { ChecklistItemId = checklistItemId, MemberId = memberId, QuantityClaimed = quantityClaimed },
                commandType: CommandType.StoredProcedure);
            return (row?.Result ?? 0, row?.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ClaimItemAsync");
            return (-1, ex.Message);
        }
    }

    public async Task<int> WithdrawClaimAsync(long checklistItemId, long memberId)
    {
        try
        {
            using var conn = Connection;
            return await conn.ExecuteAsync(
                "sp_WithdrawChecklistClaim",
                new { ChecklistItemId = checklistItemId, MemberId = memberId },
                commandType: CommandType.StoredProcedure);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WithdrawClaimAsync");
            return 0;
        }
    }

    private sealed class ChecklistResultRow
    {
        public int Result { get; set; }
        public string? Message { get; set; }
    }

    private sealed class ChecklistClaimRow
    {
        public long ChecklistItemId { get; set; }
        public long MemberId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public int QuantityClaimed { get; set; }
        public DateTime ClaimedDate { get; set; }
    }
}
