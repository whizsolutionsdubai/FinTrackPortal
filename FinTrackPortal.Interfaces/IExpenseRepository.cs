using FinTrackPortal.Common;
using FinTrackPortal.Models;

namespace FinTrackPortal.Interfaces
{
    public interface IExpenseRepository
    {
        Task<OperationResult<long>> AddExpenseAsync(long groupId, string description, decimal amount, long paidBy, string splitType, List<long> members, List<decimal>? customAmounts, string createdBy);
        Task<OperationResult<bool>> EditExpenseAsync(long expenseId, string description, decimal amount, long paidBy, string splitType, List<long> members, List<decimal>? customAmounts, string modifiedBy);
        Task<OperationResult<bool>> DeleteExpenseAsync(long expenseId, string modifiedBy);
        Task<OperationResult<List<ExpenseResponse>>> GetExpensesByGroupAsync(long groupId);
        Task<OperationResult<long>> AddPersonalExpenseAsync(string description, decimal amount, long memberId, string createdBy);
        Task<OperationResult<List<ExpenseResponse>>> GetPersonalExpensesAsync(long memberId);
    }
}
