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
    /// Dapper implementation of <see cref="ISubscriptionRepository"/>.
    /// All queries go through SQL Server stored procedures — no inline SQL.
    /// </summary>
    public class SubscriptionRepository : ISubscriptionRepository
    {
        private readonly IConfiguration _config;
        private readonly ILogger<SubscriptionRepository> _logger;

        public SubscriptionRepository(IConfiguration config, ILogger<SubscriptionRepository> logger)
        {
            _config = config;
            _logger = logger;
        }

        private IDbConnection Connection => new SqlConnection(_config.GetConnectionString("DefaultConnection"));

        public async Task<OperationResult<List<SubscriptionPlanResponse>>> GetPlansAsync()
        {
            try
            {
                using var conn = Connection;

                var plans = (await conn.QueryAsync<SubscriptionPlanResponse>(
                    "sp_GetSubscriptionPlans",
                    commandType: CommandType.StoredProcedure)).ToList();

                return OperationResult<List<SubscriptionPlanResponse>>.Success(plans);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching subscription plans");
                return OperationResult<List<SubscriptionPlanResponse>>.Failure(ex.Message);
            }
        }

        public async Task<OperationResult<UserSubscriptionResponse?>> GetUserSubscriptionAsync(long memberId)
        {
            try
            {
                using var conn = Connection;

                var sub = await conn.QueryFirstOrDefaultAsync<UserSubscriptionResponse>(
                    "sp_GetUserSubscription",
                    new { MemberId = memberId },
                    commandType: CommandType.StoredProcedure);

                return OperationResult<UserSubscriptionResponse?>.Success(sub);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching subscription for member {MemberId}", memberId);
                return OperationResult<UserSubscriptionResponse?>.Failure(ex.Message);
            }
        }

        public async Task<OperationResult<bool>> CreateSubscriptionAsync(long memberId, int planId, string billingCycle, string? paymentRef)
        {
            try
            {
                using var conn = Connection;

                await conn.ExecuteAsync(
                    "sp_CreateUserSubscription",
                    new { MemberId = memberId, PlanId = planId, BillingCycle = billingCycle, PaymentRef = paymentRef },
                    commandType: CommandType.StoredProcedure);

                return OperationResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating subscription for member {MemberId}", memberId);
                return OperationResult<bool>.Failure(ex.Message);
            }
        }

        public async Task<OperationResult<bool>> CancelSubscriptionAsync(long memberId)
        {
            try
            {
                using var conn = Connection;

                await conn.ExecuteAsync(
                    "sp_CancelUserSubscription",
                    new { MemberId = memberId },
                    commandType: CommandType.StoredProcedure);

                return OperationResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling subscription for member {MemberId}", memberId);
                return OperationResult<bool>.Failure(ex.Message);
            }
        }

        public async Task<OperationResult<bool>> IsActionAllowedAsync(long memberId, string checkType, long? groupId = null)
        {
            try
            {
                using var conn = Connection;

                var result = await conn.QueryFirstOrDefaultAsync<int>(
                    "sp_CheckUserLimit",
                    new { MemberId = memberId, CheckType = checkType, GroupId = groupId },
                    commandType: CommandType.StoredProcedure);

                return OperationResult<bool>.Success(result == 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking limit for member {MemberId}, type {CheckType}", memberId, checkType);
                return OperationResult<bool>.Failure(ex.Message);
            }
        }
    }
}
