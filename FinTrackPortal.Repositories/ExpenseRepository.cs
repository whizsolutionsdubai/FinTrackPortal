using Dapper;
using FinTrackPortal.Common;
using FinTrackPortal.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;

namespace FinTrackPortal.Repositories
{
    public class ExpenseRepository : IExpenseRepository
    {
        private readonly IConfiguration _config;
        private readonly ILogger<ExpenseRepository> _logger;

        public ExpenseRepository(IConfiguration config, ILogger<ExpenseRepository> logger)
        {
            _config = config;
            _logger = logger;
        }

        private IDbConnection Connection => new SqlConnection(_config.GetConnectionString("DefaultConnection"));

        public async Task<OperationResult<long>> AddExpenseAsync(long groupId, string description, decimal amount, long paidBy, List<long> members, string createdBy)
        {
            try
            {
                using var conn = Connection;
                conn.Open();
                using var tx = conn.BeginTransaction();

                var expenseId = await conn.QuerySingleAsync<long>(
                    "sp_AddExpense",
                    new { GroupId = groupId, Description = description, Amount = amount, PaidBy = paidBy, CreatedBy = createdBy },
                    transaction: tx,
                    commandType: CommandType.StoredProcedure);

                decimal share = Math.Round(amount / members.Count, 2);

                foreach (var memberId in members)
                {
                    await conn.ExecuteAsync(
                        "sp_AddExpenseSplit",
                        new { ExpenseId = expenseId, MemberId = memberId, ShareAmount = share, CreatedBy = createdBy },
                        transaction: tx,
                        commandType: CommandType.StoredProcedure);
                }

                tx.Commit();
                return OperationResult<long>.Success(expenseId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding expense for group {GroupId}", groupId);
                return OperationResult<long>.Failure(ex.Message);
            }
        }

        public async Task<OperationResult<bool>> EditExpenseAsync(long expenseId, string description, decimal amount, long paidBy, List<long> members, string modifiedBy)
        {
            try
            {
                using var conn = Connection;
                conn.Open();
                using var tx = conn.BeginTransaction();

                await conn.ExecuteAsync(
                    "sp_UpdateExpense",
                    new { ExpenseId = expenseId, Description = description, Amount = amount, PaidBy = paidBy, ModifiedBy = modifiedBy },
                    transaction: tx,
                    commandType: CommandType.StoredProcedure);

                await conn.ExecuteAsync(
                    "sp_DeleteExpenseSplits",
                    new { ExpenseId = expenseId },
                    transaction: tx,
                    commandType: CommandType.StoredProcedure);

                decimal share = Math.Round(amount / members.Count, 2);

                foreach (var memberId in members)
                {
                    await conn.ExecuteAsync(
                        "sp_AddExpenseSplit",
                        new { ExpenseId = expenseId, MemberId = memberId, ShareAmount = share, CreatedBy = modifiedBy },
                        transaction: tx,
                        commandType: CommandType.StoredProcedure);
                }

                tx.Commit();
                return OperationResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing expense {ExpenseId}", expenseId);
                return OperationResult<bool>.Failure(ex.Message);
            }
        }

        public async Task<OperationResult<bool>> DeleteExpenseAsync(long expenseId, string modifiedBy)
        {
            try
            {
                using var conn = Connection;

                await conn.ExecuteAsync(
                    "sp_DeleteExpense",
                    new { ExpenseId = expenseId, ModifiedBy = modifiedBy },
                    commandType: CommandType.StoredProcedure);

                return OperationResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting expense {ExpenseId}", expenseId);
                return OperationResult<bool>.Failure(ex.Message);
            }
        }
    }
}
