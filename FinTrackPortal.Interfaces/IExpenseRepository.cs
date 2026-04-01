using FinTrackPortal.Common;

namespace FinTrackPortal.Interfaces
{
    public interface IExpenseRepository
    {
        Task<OperationResult<long>> AddExpenseAsync(long groupId, string description, decimal amount, long paidBy, List<long> members, string createdBy);
        Task<OperationResult<bool>> EditExpenseAsync(long expenseId, string description, decimal amount, long paidBy, List<long> members, string modifiedBy);
        Task<OperationResult<bool>> DeleteExpenseAsync(long expenseId, string modifiedBy);
    }
}
