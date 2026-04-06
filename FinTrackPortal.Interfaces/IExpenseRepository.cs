using FinTrackPortal.Common;
using FinTrackPortal.Models;

namespace FinTrackPortal.Interfaces
{
    /// <summary>
    /// Data-access contract for group and personal expense operations.
    /// Implemented by ExpenseRepository using Dapper + stored procedures.
    /// Multi-step operations (add/edit) use C#-level transactions.
    /// </summary>
    public interface IExpenseRepository
    {
        /// <summary>Add a group expense and its splits in a single C# transaction (sp_AddExpense + sp_AddExpenseSplit).</summary>
        Task<OperationResult<long>> AddExpenseAsync(long groupId, string description, decimal amount, string currencyCode, long paidBy, string splitType, List<long> members, List<decimal>? customAmounts, string createdBy);

        /// <summary>Update expense details and rebuild all splits (sp_UpdateExpense + sp_DeleteExpenseSplits + sp_AddExpenseSplit).</summary>
        Task<OperationResult<bool>> EditExpenseAsync(long expenseId, string description, decimal amount, string? currencyCode, long paidBy, string splitType, List<long> members, List<decimal>? customAmounts, string modifiedBy, string? expenseCategory, string? forReference);

        /// <summary>Soft-delete an expense and its splits (sp_DeleteExpense sets IsActive = 0).</summary>
        Task<OperationResult<bool>> DeleteExpenseAsync(long expenseId, string modifiedBy);

        /// <summary>List all active expenses for a group (sp_GetExpensesByGroup).</summary>
        Task<OperationResult<List<ExpenseResponse>>> GetExpensesByGroupAsync(long groupId);

        /// <summary>Add a personal (non-group) expense — stored with GroupId = NULL (sp_AddPersonalExpense).</summary>
        Task<OperationResult<long>> AddPersonalExpenseAsync(string description, decimal amount, string currencyCode, long memberId, string createdBy, DateTime? expenseDate, long? accountId, string? expenseCategory, string? forReference);

        /// <summary>List personal/office expenses for a member; optional category filter (sp_GetPersonalExpenses).</summary>
        Task<OperationResult<List<ExpenseResponse>>> GetPersonalExpensesAsync(long memberId, string? category);

        /// <summary>Update a personal/office expense owned by the member (sp_UpdatePersonalExpense).</summary>
        Task<OperationResult<bool>> UpdatePersonalExpenseAsync(long expenseId, long memberId, string description, decimal amount, string? currencyCode, DateTime? expenseDate, long? accountId, string? expenseCategory, string? forReference, string modifiedBy);

        /// <summary>Move an expense to a different group (sp_MoveExpense). Splits remain unchanged.</summary>
        Task<OperationResult<bool>> MoveExpenseAsync(long expenseId, long newGroupId, string modifiedBy);

        /// <summary>Get all account labels for a user (sp_GetUserAccounts).</summary>
        Task<OperationResult<List<ExpenseAccountResponse>>> GetUserAccountsAsync(long userId);

        /// <summary>Create a new account label (sp_CreateAccount).</summary>
        Task<OperationResult<long>> CreateAccountAsync(long userId, string accountName, string? accountColor);

        /// <summary>Soft-delete an account label (sp_DeleteAccount).</summary>
        Task<OperationResult<bool>> DeleteAccountAsync(long accountId);

        /// <summary>Save attachment metadata after the file is uploaded to blob storage (sp_AddExpenseAttachment).</summary>
        Task<OperationResult<long>> AddAttachmentAsync(long expenseId, string fileName, string fileUrl, string fileType, int? fileSizeKB, string uploadedBy);

        /// <summary>Get all active attachments for an expense (sp_GetExpenseAttachments).</summary>
        Task<OperationResult<List<AttachmentResponse>>> GetAttachmentsAsync(long expenseId);

        /// <summary>All attachments uploaded by the member with expense context (sp_GetAttachmentsByMember).</summary>
        Task<OperationResult<List<MemberAttachmentResponse>>> GetAllAttachmentsAsync(long memberId);

        /// <summary>Soft-delete an attachment record (sp_DeleteExpenseAttachment).</summary>
        Task<OperationResult<bool>> DeleteAttachmentAsync(long attachmentId);

        /// <summary>Record or replace a payer line for an expense (sp_AddExpensePayer).</summary>
        Task<OperationResult<long>> AddExpensePayerAsync(AddExpensePayerRequest request);

        /// <summary>List payers for an expense (sp_GetExpensePayers).</summary>
        Task<OperationResult<List<ExpensePayerResponse>>> GetExpensePayersAsync(long expenseId);
    }
}
