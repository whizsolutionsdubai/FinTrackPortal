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
    /// Dapper implementation of <see cref="IGroupRepository"/>.
    /// All queries go through SQL Server stored procedures — no inline SQL.
    /// SP row shapes for <c>sp_GetGroupSummary</c>: <c>FinTrackPortal.Models/Data/GroupRepositorySpResultRows.cs</c>.
    /// </summary>
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

                string groupCode = GenerateGroupCode(groupName);

                var groupId = await conn.QuerySingleAsync<long>(
                    "sp_CreateGroup",
                    new { GroupName = groupName, GroupCode = groupCode, CreatedByMemberId = createdByMemberId, CreatedBy = createdBy },
                    commandType: CommandType.StoredProcedure);

                return OperationResult<long>.Success(groupId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating group {GroupName} for member {MemberId}", groupName, createdByMemberId);
                return OperationResult<long>.Failure(ex.Message);
            }
        }

        public async Task<OperationResult<bool>> AddMemberToGroupAsync(long groupId, long memberId, string role, string createdBy)
        {
            try
            {
                using var conn = Connection;

                await conn.ExecuteAsync(
                    "sp_AddMemberToGroup",
                    new { GroupId = groupId, MemberId = memberId, Role = role, CreatedBy = createdBy },
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

                var rows = (await conn.QueryAsync<GroupSummarySpRow>(
                    "sp_GetGroupSummary",
                    new { GroupId = groupId },
                    commandType: CommandType.StoredProcedure)).ToList();

                if (rows.Count == 0)
                    return OperationResult<GroupSummaryResponse>.Failure("Group not found or has no members.");

                var response = new GroupSummaryResponse
                {
                    GroupId = groupId,
                    GroupName = rows[0].GroupName,
                    GroupCode = rows[0].GroupCode,
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

        public async Task<OperationResult<List<MyGroupResponse>>> GetMyGroupsAsync(long memberId)
        {
            try
            {
                using var conn = Connection;

                var groups = (await conn.QueryAsync<MyGroupResponse>(
                    "sp_GetMyGroups",
                    new { MemberId = memberId },
                    commandType: CommandType.StoredProcedure)).ToList();

                return OperationResult<List<MyGroupResponse>>.Success(groups);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching groups for member {MemberId}", memberId);
                return OperationResult<List<MyGroupResponse>>.Failure(ex.Message);
            }
        }

        public async Task<OperationResult<List<GroupMemberResponse>>> GetGroupMembersAsync(long groupId)
        {
            try
            {
                using var conn = Connection;

                var members = (await conn.QueryAsync<GroupMemberResponse>(
                    "sp_GetGroupMembers",
                    new { GroupId = groupId },
                    commandType: CommandType.StoredProcedure)).ToList();

                return OperationResult<List<GroupMemberResponse>>.Success(members);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching members for group {GroupId}", groupId);
                return OperationResult<List<GroupMemberResponse>>.Failure(ex.Message);
            }
        }

        public async Task<OperationResult<bool>> IsMemberOfGroupAsync(long groupId, long memberId)
        {
            try
            {
                using var conn = Connection;

                var exists = await conn.QuerySingleOrDefaultAsync<int>(
                    "sp_IsMemberOfGroup",
                    new { GroupId = groupId, MemberId = memberId },
                    commandType: CommandType.StoredProcedure);

                return OperationResult<bool>.Success(exists == 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking membership for member {MemberId} in group {GroupId}", memberId, groupId);
                return OperationResult<bool>.Failure(ex.Message);
            }
        }

        /// <summary>
        /// Generate a short group code from the group name initials + 4 random hex chars.
        /// Example: "Dubai Friends" → "DF-A1B2".
        /// </summary>
        private static string GenerateGroupCode(string groupName)
        {
            var words = groupName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string initials = words.Length >= 2
                ? $"{char.ToUpper(words[0][0])}{char.ToUpper(words[1][0])}"
                : groupName.Length >= 2
                    ? $"{char.ToUpper(groupName[0])}{char.ToUpper(groupName[1])}"
                    : $"{char.ToUpper(groupName[0])}X";

            string random = Guid.NewGuid().ToString("N")[..4].ToUpper();
            return $"{initials}-{random}";
        }

    }
}
