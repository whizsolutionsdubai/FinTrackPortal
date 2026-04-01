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
    public class GroupRepository : IGroupRepository
    {
        private readonly IConfiguration _config;
        private readonly ILogger<GroupRepository> _logger;

        public GroupRepository(IConfiguration config, ILogger<GroupRepository> logger)
        {
            _config = config;
            _logger = logger;
        }

        private IDbConnection Connection => new SqlConnection(_config.GetConnectionString("DefaultConnection"));

        public async Task<OperationResult<long>> CreateGroupAsync(string groupName, long createdByMemberId, string createdBy)
        {
            try
            {
                using var conn = Connection;

                var groupId = await conn.QuerySingleAsync<long>(
                    "sp_CreateGroup",
                    new { GroupName = groupName, CreatedByMemberId = createdByMemberId, CreatedBy = createdBy },
                    commandType: CommandType.StoredProcedure);

                return OperationResult<long>.Success(groupId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating group {GroupName} for member {MemberId}", groupName, createdByMemberId);
                return OperationResult<long>.Failure(ex.Message);
            }
        }

        public async Task<OperationResult<bool>> AddMemberToGroupAsync(long groupId, long memberId, string createdBy)
        {
            try
            {
                using var conn = Connection;

                await conn.ExecuteAsync(
                    "sp_AddMemberToGroup",
                    new { GroupId = groupId, MemberId = memberId, CreatedBy = createdBy },
                    commandType: CommandType.StoredProcedure);

                return OperationResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding member {MemberId} to group {GroupId}", memberId, groupId);
                return OperationResult<bool>.Failure(ex.Message);
            }
        }

        public async Task<OperationResult<GroupSummaryResponse>> GetGroupSummaryAsync(long groupId)
        {
            try
            {
                using var conn = Connection;

                var rows = (await conn.QueryAsync<GroupSummaryRow>(
                    "sp_GetGroupSummary",
                    new { GroupId = groupId },
                    commandType: CommandType.StoredProcedure)).ToList();

                if (rows.Count == 0)
                    return OperationResult<GroupSummaryResponse>.Failure("Group not found or has no members.");

                var response = new GroupSummaryResponse
                {
                    GroupId = groupId,
                    GroupName = rows[0].GroupName,
                    Members = rows.Select(r => new MemberSummary
                    {
                        MemberId = r.MemberId,
                        Name = r.Name,
                        Paid = r.Paid,
                        Share = r.Share,
                        Net = r.Net
                    }).ToList()
                };

                return OperationResult<GroupSummaryResponse>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching summary for group {GroupId}", groupId);
                return OperationResult<GroupSummaryResponse>.Failure(ex.Message);
            }
        }

        private class GroupSummaryRow
        {
            public string GroupName { get; set; } = string.Empty;
            public long MemberId { get; set; }
            public string Name { get; set; } = string.Empty;
            public decimal Paid { get; set; }
            public decimal Share { get; set; }
            public decimal Net { get; set; }
        }
    }
}
