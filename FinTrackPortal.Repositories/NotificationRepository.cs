using Dapper;
using FinTrackPortal.Interfaces;
using FinTrackPortal.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;

namespace FinTrackPortal.Repositories;

public sealed class NotificationRepository : INotificationRepository
{
    private readonly IConfiguration _config;
    private readonly ILogger<NotificationRepository> _logger;

    public NotificationRepository(IConfiguration config, ILogger<NotificationRepository> logger)
    {
        _config = config;
        _logger = logger;
    }

    private IDbConnection Connection => new SqlConnection(_config.GetConnectionString("DefaultConnection"));

    public async Task<IReadOnlyList<NotificationItem>> GetForMemberAsync(long memberId, bool unreadOnly, int take)
    {
        try
        {
            using var conn = Connection;
            var rows = await conn.QueryAsync<NotificationItem>(
                "sp_GetNotifications",
                new { MemberId = memberId, UnreadOnly = unreadOnly ? 1 : 0, Take = take },
                commandType: CommandType.StoredProcedure);
            return rows.AsList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetForMemberAsync notifications");
            return Array.Empty<NotificationItem>();
        }
    }

    public async Task<int> MarkReadAsync(long notificationId, long memberId)
    {
        try
        {
            using var conn = Connection;
            var row = await conn.QueryFirstOrDefaultAsync<SpRowsUpdatedRow>(
                "sp_MarkNotificationRead",
                new { NotificationId = notificationId, MemberId = memberId },
                commandType: CommandType.StoredProcedure);
            return row?.RowsUpdated ?? 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MarkReadAsync");
            return 0;
        }
    }

    public async Task MarkAllReadAsync(long memberId)
    {
        try
        {
            using var conn = Connection;
            await conn.ExecuteAsync(
                "sp_MarkAllNotificationsRead",
                new { MemberId = memberId },
                commandType: CommandType.StoredProcedure);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MarkAllReadAsync");
        }
    }

    public async Task<long> CreateAsync(long memberId, string title, string? body, string? notificationType, string? linkUrl)
    {
        try
        {
            using var conn = Connection;
            var id = await conn.QuerySingleAsync<long>(
                "sp_CreateNotification",
                new { MemberId = memberId, Title = title, Body = body, NotificationType = notificationType, LinkUrl = linkUrl },
                commandType: CommandType.StoredProcedure);
            return id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateAsync notification");
            return 0;
        }
    }
}
