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
    /// Dapper implementation of <see cref="IExpenseRepository"/>.
    /// Add and Edit use C#-level transactions to atomically write expense + splits.
    /// All queries go through SQL Server stored procedures — no inline SQL.
    /// </summary>
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

        public async Task<OperationResult<long>> AddExpenseAsync(long groupId, string description, decimal amount, string currencyCode, long paidBy, string splitType, List<long> members, List<decimal>? customAmounts, string createdBy)
        {
            try
            {
                using var conn = Connection;
                conn.Open();
                using var tx = conn.BeginTransaction();

                var expenseId = await conn.QuerySingleAsync<long>(
                    "sp_AddExpense",
                    new { GroupId = groupId, Description = description, Amount = amount, CurrencyCode = currencyCode, PaidBy = paidBy, SplitType = splitType, CreatedBy = createdBy },
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

        public async Task<OperationResult<bool>> EditExpenseAsync(long expenseId, string description, decimal amount, string? currencyCode, long paidBy, string splitType, List<long> members, List<decimal>? customAmounts, string modifiedBy, string? expenseCategory, string? forReference)
        {
            try
            {
                using var conn = Connection;
                conn.Open();
                using var tx = conn.BeginTransaction();

                await conn.ExecuteAsync(
                    "sp_UpdateExpense",
                    new
                    {
                        ExpenseId = expenseId,
                        Description = description,
                        Amount = amount,
                        CurrencyCode = currencyCode,
                        PaidBy = paidBy,
                        SplitType = splitType,
                        ModifiedBy = modifiedBy,
                        AccountId = (long?)null,
                        ExpenseCategory = expenseCategory,
                        ForReference = forReference
                    },
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

        public async Task<OperationResult<long>> AddPersonalExpenseAsync(string description, decimal amount, string currencyCode, long memberId, string createdBy, DateTime? expenseDate, long? accountId, string? expenseCategory, string? forReference)
        {
            try
            {
                using var conn = Connection;

                var expenseId = await conn.QuerySingleAsync<long>(
                    "sp_AddPersonalExpense",
                    new
                    {
                        Description = description,
                        Amount = amount,
                        CurrencyCode = currencyCode,
                        MemberId = memberId,
                        CreatedBy = createdBy,
                        ExpenseDate = expenseDate.HasValue ? expenseDate.Value.Date : (DateTime?)null,
                        AccountId = accountId,
                        ExpenseCategory = expenseCategory,
                        ForReference = forReference
                    },
                    commandType: CommandType.StoredProcedure);

                return OperationResult<long>.Success(expenseId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding personal expense for member {MemberId}", memberId);
                return OperationResult<long>.Failure(ex.Message);
            }
        }

        public async Task<OperationResult<bool>> UpdatePersonalExpenseAsync(long expenseId, long memberId, string description, decimal amount, string? currencyCode, DateTime? expenseDate, long? accountId, string? expenseCategory, string? forReference, string modifiedBy)
        {
            try
            {
                using var conn = Connection;

                var rows = await conn.ExecuteAsync(
                    "sp_UpdatePersonalExpense",
                    new
                    {
                        ExpenseId = expenseId,
                        MemberId = memberId,
                        Description = description,
                        Amount = amount,
                        CurrencyCode = currencyCode,
                        ExpenseDate = expenseDate.HasValue ? expenseDate.Value.Date : (DateTime?)null,
                        AccountId = accountId,
                        ExpenseCategory = expenseCategory,
                        ForReference = forReference,
                        ModifiedBy = modifiedBy
                    },
                    commandType: CommandType.StoredProcedure);

                return rows > 0
                    ? OperationResult<bool>.Success(true)
                    : OperationResult<bool>.Failure("Personal expense not found or access denied.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating personal expense {ExpenseId}", expenseId);
                return OperationResult<bool>.Failure(ex.Message);
            }
        }

        public async Task<OperationResult<List<ExpenseResponse>>> GetPersonalExpensesAsync(long memberId, string? category)
        {
            try
            {
                using var conn = Connection;

                var expenses = (await conn.QueryAsync<ExpenseResponse>(
                    "sp_GetPersonalExpenses",
                    new { MemberId = memberId, Category = category },
                    commandType: CommandType.StoredProcedure)).ToList();

                return OperationResult<List<ExpenseResponse>>.Success(expenses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching personal expenses for member {MemberId}", memberId);
                return OperationResult<List<ExpenseResponse>>.Failure(ex.Message);
            }
        }

        public async Task<OperationResult<bool>> MoveExpenseAsync(long expenseId, long newGroupId, string modifiedBy)
        {
            try
            {
                using var conn = Connection;

                var rows = await conn.ExecuteAsync(
                    "sp_MoveExpense",
                    new { ExpenseId = expenseId, NewGroupId = newGroupId, ModifiedBy = modifiedBy },
                    commandType: CommandType.StoredProcedure);

                return rows > 0
                    ? OperationResult<bool>.Success(true)
                    : OperationResult<bool>.Failure("Expense not found or already inactive.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving expense {ExpenseId} to group {NewGroupId}", expenseId, newGroupId);
                return OperationResult<bool>.Failure(ex.Message);
            }
        }

        public async Task<OperationResult<List<ExpenseAccountResponse>>> GetUserAccountsAsync(long userId)
        {
            try
            {
                using var conn = Connection;

                var accounts = (await conn.QueryAsync<ExpenseAccountResponse>(
                    "sp_GetUserAccounts",
                    new { UserId = userId },
                    commandType: CommandType.StoredProcedure)).ToList();

                return OperationResult<List<ExpenseAccountResponse>>.Success(accounts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching accounts for user {UserId}", userId);
                return OperationResult<List<ExpenseAccountResponse>>.Failure(ex.Message);
            }
        }

        public async Task<OperationResult<long>> CreateAccountAsync(long userId, string accountName, string? accountColor)
        {
            try
            {
                using var conn = Connection;

                var accountId = await conn.ExecuteScalarAsync<long>(
                    "sp_CreateAccount",
                    new { UserId = userId, AccountName = accountName, AccountColor = accountColor },
                    commandType: CommandType.StoredProcedure);

                return OperationResult<long>.Success(accountId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating account for user {UserId}", userId);
                return OperationResult<long>.Failure(ex.Message);
            }
        }

        public async Task<OperationResult<bool>> DeleteAccountAsync(long accountId)
        {
            try
            {
                using var conn = Connection;

                await conn.ExecuteAsync(
                    "sp_DeleteAccount",
                    new { AccountId = accountId },
                    commandType: CommandType.StoredProcedure);

                return OperationResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting account {AccountId}", accountId);
                return OperationResult<bool>.Failure(ex.Message);
            }
        }

        public async Task<OperationResult<long>> AddAttachmentAsync(long expenseId, string fileName, string fileUrl, string fileType, int? fileSizeKB, string uploadedBy)
        {
            try
            {
                using var conn = Connection;

                var attachmentId = await conn.ExecuteScalarAsync<long>(
                    "sp_AddExpenseAttachment",
                    new { ExpenseId = expenseId, FileName = fileName, FileUrl = fileUrl, FileType = fileType, FileSizeKB = fileSizeKB, UploadedBy = uploadedBy },
                    commandType: CommandType.StoredProcedure);

                return OperationResult<long>.Success(attachmentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding attachment for expense {ExpenseId}", expenseId);
                return OperationResult<long>.Failure(ex.Message);
            }
        }

        public async Task<OperationResult<List<AttachmentResponse>>> GetAttachmentsAsync(long expenseId)
        {
            try
            {
                using var conn = Connection;

                var attachments = (await conn.QueryAsync<AttachmentResponse>(
                    "sp_GetExpenseAttachments",
                    new { ExpenseId = expenseId },
                    commandType: CommandType.StoredProcedure)).ToList();

                return OperationResult<List<AttachmentResponse>>.Success(attachments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching attachments for expense {ExpenseId}", expenseId);
                return OperationResult<List<AttachmentResponse>>.Failure(ex.Message);
            }
        }

        public async Task<OperationResult<List<MemberAttachmentResponse>>> GetAllAttachmentsAsync(long memberId)
        {
            try
            {
                using var conn = Connection;

                var list = (await conn.QueryAsync<MemberAttachmentResponse>(
                    "sp_GetAttachmentsByMember",
                    new { MemberId = memberId },
                    commandType: CommandType.StoredProcedure)).ToList();

                return OperationResult<List<MemberAttachmentResponse>>.Success(list);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching attachments for member {MemberId}", memberId);
                return OperationResult<List<MemberAttachmentResponse>>.Failure(ex.Message);
            }
        }

        public async Task<OperationResult<long>> AddExpensePayerAsync(AddExpensePayerRequest request)
        {
            try
            {
                using var conn = Connection;

                var payerId = await conn.ExecuteScalarAsync<long>(
                    "sp_AddExpensePayer",
                    new { request.ExpenseId, request.MemberId, request.AmountPaid },
                    commandType: CommandType.StoredProcedure);

                return OperationResult<long>.Success(payerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding payer for expense {ExpenseId}", request.ExpenseId);
                return OperationResult<long>.Failure(ex.Message);
            }
        }

        public async Task<OperationResult<List<ExpensePayerResponse>>> GetExpensePayersAsync(long expenseId)
        {
            try
            {
                using var conn = Connection;

                var payers = (await conn.QueryAsync<ExpensePayerResponse>(
                    "sp_GetExpensePayers",
                    new { ExpenseId = expenseId },
                    commandType: CommandType.StoredProcedure)).ToList();

                return OperationResult<List<ExpensePayerResponse>>.Success(payers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching payers for expense {ExpenseId}", expenseId);
                return OperationResult<List<ExpensePayerResponse>>.Failure(ex.Message);
            }
        }

        public async Task<OperationResult<bool>> DeleteAttachmentAsync(long attachmentId)
        {
            try
            {
                using var conn = Connection;

                await conn.ExecuteAsync(
                    "sp_DeleteExpenseAttachment",
                    new { AttachmentId = attachmentId },
                    commandType: CommandType.StoredProcedure);

                return OperationResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting attachment {AttachmentId}", attachmentId);
                return OperationResult<bool>.Failure(ex.Message);
            }
        }

        /// <summary>
        /// Split an expense amount among members.
        /// "Custom" uses the caller-supplied amounts; "Equal" divides evenly (rounded to 2 decimals).
        /// </summary>
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
