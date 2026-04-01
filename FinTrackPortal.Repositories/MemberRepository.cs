using Dapper;
using FinTrackPortal.Common;
using FinTrackPortal.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;

namespace FinTrackPortal.Repositories
{
    public class MemberRepository : IMemberRepository
    {
        private readonly IConfiguration _config;
        private readonly ILogger<MemberRepository> _logger;

        public MemberRepository(IConfiguration config, ILogger<MemberRepository> logger)
        {
            _config = config;
            _logger = logger;
        }

        private IDbConnection Connection => new SqlConnection(_config.GetConnectionString("DefaultConnection"));

        public async Task<OperationResult<long>> CreateMemberAsync(string memberName, string createdBy)
        {
            try
            {
                using var conn = Connection;

                var memberId = await conn.QuerySingleAsync<long>(
                    "sp_CreateMember",
                    new { MemberName = memberName, CreatedBy = createdBy },
                    commandType: CommandType.StoredProcedure);

                return OperationResult<long>.Success(memberId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating member {MemberName}", memberName);
                return OperationResult<long>.Failure(ex.Message);
            }
        }

        public async Task<OperationResult<bool>> EditMemberAsync(long memberId, string memberName, string modifiedBy)
        {
            try
            {
                using var conn = Connection;

                var rowsAffected = await conn.ExecuteAsync(
                    "sp_EditMember",
                    new { MemberId = memberId, MemberName = memberName, ModifiedBy = modifiedBy },
                    commandType: CommandType.StoredProcedure);

                if (rowsAffected == 0)
                    return OperationResult<bool>.Failure("Member not found.");

                return OperationResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing member {MemberId}", memberId);
                return OperationResult<bool>.Failure(ex.Message);
            }
        }

        public async Task<OperationResult<bool>> DeleteMemberAsync(long memberId, string modifiedBy)
        {
            try
            {
                using var conn = Connection;

                var rowsAffected = await conn.ExecuteAsync(
                    "sp_DeleteMember",
                    new { MemberId = memberId, ModifiedBy = modifiedBy },
                    commandType: CommandType.StoredProcedure);

                if (rowsAffected == 0)
                    return OperationResult<bool>.Failure("Member not found.");

                return OperationResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting member {MemberId}", memberId);
                return OperationResult<bool>.Failure(ex.Message);
            }
        }
    }
}
