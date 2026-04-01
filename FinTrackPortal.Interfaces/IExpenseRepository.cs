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
        Task<OperationResult<long>> AddExpenseAsync(long groupId, string description, decimal amount, long paidBy, string splitType, List<long> members, List<decimal>? customAmounts, string createdBy);

        /// <summary>Update expense details and rebuild all splits (sp_UpdateExpense + sp_DeleteExpenseSplits + sp_AddExpenseSplit).</summary>
        Task<OperationResult<bool>> EditExpenseAsync(long expenseId, string description, decimal amount, long paidBy, string splitType, List<long> members, List<decimal>? customAmounts, string modifiedBy);

        /// <summary>Soft-delete an expense and its splits (sp_DeleteExpense sets IsActive = 0).</summary>
        Task<OperationResult<bool>> DeleteExpenseAsync(long expenseId, string modifiedBy);

        /// <summary>List all active expenses for a group (sp_GetExpensesByGroup).</summary>
        Task<OperationResult<List<ExpenseResponse>>> GetExpensesByGroupAsync(long groupId);

        /// <summary>Add a personal (non-group) expense — stored with GroupId = NULL (sp_AddPersonalExpense).</summary>
        Task<OperationResult<long>> AddPersonalExpenseAsync(string description, decimal amount, long memberId, string createdBy);

        /// <summary>List all personal expenses for a member (sp_GetPersonalExpenses).</summary>
        Task<OperationResult<List<ExpenseResponse>>> GetPersonalExpensesAsync(long memberId);
    }
}
