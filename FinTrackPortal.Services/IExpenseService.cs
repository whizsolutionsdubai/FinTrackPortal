using FinTrackPortal.Common;
using FinTrackPortal.Models;

namespace FinTrackPortal.Services
{
    /// <summary>
    /// Business-logic contract for expense operations.
    /// Currently a thin pass-through to <see cref="Interfaces.IExpenseRepository"/>.
    /// </summary>
    public interface IExpenseService
    {
        Task<OperationResult<long>> AddExpenseAsync(long groupId, string description, decimal amount, long paidBy, string splitType, List<long> members, List<decimal>? customAmounts, string createdBy);
        Task<OperationResult<bool>> EditExpenseAsync(long expenseId, string description, decimal amount, long paidBy, string splitType, List<long> members, List<decimal>? customAmounts, string modifiedBy);
        Task<OperationResult<bool>> DeleteExpenseAsync(long expenseId, string modifiedBy);
        Task<OperationResult<List<ExpenseResponse>>> GetExpensesByGroupAsync(long groupId);
        Task<OperationResult<long>> AddPersonalExpenseAsync(string description, decimal amount, long memberId, string createdBy);
        Task<OperationResult<List<ExpenseResponse>>> GetPersonalExpensesAsync(long memberId);
        Task<OperationResult<bool>> MoveExpenseAsync(long expenseId, long newGroupId, string modifiedBy);

        Task<OperationResult<List<ExpenseAccountResponse>>> GetUserAccountsAsync(long userId);
        Task<OperationResult<long>> CreateAccountAsync(long userId, string accountName, string? accountColor);
        Task<OperationResult<bool>> DeleteAccountAsync(long accountId);

        Task<OperationResult<long>> AddAttachmentAsync(long expenseId, string fileName, string fileUrl, string fileType, int? fileSizeKB, string uploadedBy);
        Task<OperationResult<List<AttachmentResponse>>> GetAttachmentsAsync(long expenseId);
        Task<OperationResult<bool>> DeleteAttachmentAsync(long attachmentId);
    }
}
