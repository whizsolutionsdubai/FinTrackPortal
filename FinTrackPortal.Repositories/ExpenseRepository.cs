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

        public async Task<OperationResult<long>> AddExpenseAsync(long groupId, string description, decimal amount, long paidBy, string splitType, List<long> members, List<decimal>? customAmounts, string createdBy)
        {
            try
            {
                using var conn = Connection;
                conn.Open();
                using var tx = conn.BeginTransaction();

                var expenseId = await conn.QuerySingleAsync<long>(
                    "sp_AddExpense",
                    new { GroupId = groupId, Description = description, Amount = amount, PaidBy = paidBy, SplitType = splitType, CreatedBy = createdBy },
                    transaction: tx,
                    commandType: CommandType.StoredProcedure);

                var shares = CalculateShares(amount, splitType, members, customAmounts);

                for (int i = 0; i < members.Count; i++)
                {
                    await conn.ExecuteAsync(
                        "sp_AddExpenseSplit",
                        new { ExpenseId = expenseId, MemberId = members[i], ShareAmount = shares[i], CreatedBy = createdBy },
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

        public async Task<OperationResult<bool>> EditExpenseAsync(long expenseId, string description, decimal amount, long paidBy, string splitType, List<long> members, List<decimal>? customAmounts, string modifiedBy)
        {
            try
            {
                using var conn = Connection;
                conn.Open();
                using var tx = conn.BeginTransaction();

                await conn.ExecuteAsync(
                    "sp_UpdateExpense",
                    new { ExpenseId = expenseId, Description = description, Amount = amount, PaidBy = paidBy, SplitType = splitType, ModifiedBy = modifiedBy },
                    transaction: tx,
                    commandType: CommandType.StoredProcedure);

                await conn.ExecuteAsync(
                    "sp_DeleteExpenseSplits",
                    new { ExpenseId = expenseId },
                    transaction: tx,
                    commandType: CommandType.StoredProcedure);

                var shares = CalculateShares(amount, splitType, members, customAmounts);

                for (int i = 0; i < members.Count; i++)
                {
                    await conn.ExecuteAsync(
                        "sp_AddExpenseSplit",
                        new { ExpenseId = expenseId, MemberId = members[i], ShareAmount = shares[i], CreatedBy = modifiedBy },
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

        public async Task<OperationResult<List<ExpenseResponse>>> GetExpensesByGroupAsync(long groupId)
        {
            try
            {
                using var conn = Connection;

                var expenses = (await conn.QueryAsync<ExpenseResponse>(
                    "sp_GetExpensesByGroup",
                    new { GroupId = groupId },
                    commandType: CommandType.StoredProcedure)).ToList();

                return OperationResult<List<ExpenseResponse>>.Success(expenses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching expenses for group {GroupId}", groupId);
                return OperationResult<List<ExpenseResponse>>.Failure(ex.Message);
            }
        }

        public async Task<OperationResult<long>> AddPersonalExpenseAsync(string description, decimal amount, long memberId, string createdBy)
        {
            try
            {
                using var conn = Connection;

                var expenseId = await conn.QuerySingleAsync<long>(
                    "sp_AddPersonalExpense",
                    new { Description = description, Amount = amount, MemberId = memberId, CreatedBy = createdBy },
                    commandType: CommandType.StoredProcedure);

                return OperationResult<long>.Success(expenseId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding personal expense for member {MemberId}", memberId);
                return OperationResult<long>.Failure(ex.Message);
            }
        }

        public async Task<OperationResult<List<ExpenseResponse>>> GetPersonalExpensesAsync(long memberId)
        {
            try
            {
                using var conn = Connection;

                var expenses = (await conn.QueryAsync<ExpenseResponse>(
                    "sp_GetPersonalExpenses",
                    new { MemberId = memberId },
                    commandType: CommandType.StoredProcedure)).ToList();

                return OperationResult<List<ExpenseResponse>>.Success(expenses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching personal expenses for member {MemberId}", memberId);
                return OperationResult<List<ExpenseResponse>>.Failure(ex.Message);
            }
        }

        private static List<decimal> CalculateShares(decimal amount, string splitType, List<long> members, List<decimal>? customAmounts)
        {
            if (string.Equals(splitType, "Custom", StringComparison.OrdinalIgnoreCase)
                && customAmounts != null
                && customAmounts.Count == members.Count)
            {
                return customAmounts;
            }

            decimal share = Math.Round(amount / members.Count, 2);
            return Enumerable.Repeat(share, members.Count).ToList();
        }
    }
}
