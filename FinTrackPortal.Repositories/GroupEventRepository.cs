using Dapper;
using FinTrackPortal.Interfaces;
using FinTrackPortal.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;

namespace FinTrackPortal.Repositories;

public sealed class GroupEventRepository : IGroupEventRepository
{
    private readonly IConfiguration _config;
    private readonly ILogger<GroupEventRepository> _logger;

    public GroupEventRepository(IConfiguration config, ILogger<GroupEventRepository> logger)
    {
        _config = config;
        _logger = logger;
    }

    private IDbConnection Connection => new SqlConnection(_config.GetConnectionString("DefaultConnection"));

    public async Task<IReadOnlyList<GroupEventResponse>> GetForGroupAsync(long groupId, long requestingMemberId)
    {
        try
        {
            using var conn = Connection;
            var rows = await conn.QueryAsync<GroupEventResponse>(
                "sp_GetGroupEvents",
                new { GroupId = groupId, RequestingMemberId = requestingMemberId },
                commandType: CommandType.StoredProcedure);
            return rows.AsList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetForGroupAsync events");
            return Array.Empty<GroupEventResponse>();
        }
    }

    public async Task<long?> CreateAsync(long groupId, long createdByMemberId, string title, string? description, DateTime eventDate, string? location)
    {
        try
        {
            using var conn = Connection;
            var id = await conn.QueryFirstOrDefaultAsync<long?>(
                "sp_CreateGroupEvent",
                new
                {
                    GroupId = groupId,
                    CreatedByMemberId = createdByMemberId,
                    Title = title,
                    Description = description,
                    EventDate = eventDate,
                    Location = location
                },
                commandType: CommandType.StoredProcedure);
            return id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateAsync group event");
            return null;
        }
    }

    public async Task<int> UpdateAsync(long groupId, long eventId, long requestingMemberId, string title, string? description, DateTime eventDate, string? location)
    {
        try
        {
            using var conn = Connection;
            var n = await conn.QueryFirstOrDefaultAsync<int?>(
                "sp_UpdateGroupEvent",
                new
                {
                    GroupId = groupId,
                    EventId = eventId,
                    RequestingMemberId = requestingMemberId,
                    Title = title,
                    Description = description,
                    EventDate = eventDate,
                    Location = location
                },
                commandType: CommandType.StoredProcedure);
            return n ?? 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateAsync group event");
            return 0;
        }
    }

    public async Task<int> DeleteAsync(long groupId, long eventId, long requestingMemberId)
    {
        try
        {
            using var conn = Connection;
            var n = await conn.QueryFirstOrDefaultAsync<int?>(
                "sp_DeleteGroupEvent",
                new { GroupId = groupId, EventId = eventId, RequestingMemberId = requestingMemberId },
                commandType: CommandType.StoredProcedure);
            return n ?? 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteAsync group event");
            return 0;
        }
    }
}
