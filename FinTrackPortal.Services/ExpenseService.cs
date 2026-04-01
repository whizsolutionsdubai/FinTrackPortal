using FinTrackPortal.Common;
using FinTrackPortal.Interfaces;
using FinTrackPortal.Models;

namespace FinTrackPortal.Services
{
    /// <summary>Delegates all expense operations to <see cref="IExpenseRepository"/>.</summary>
    public class ExpenseService : IExpenseService
    {
        private readonly IExpenseRepository _repository;

        public ExpenseService(IExpenseRepository repository)
        {
            _repository = repository;
        }

        public Task<OperationResult<long>> AddExpenseAsync(long groupId, string description, decimal amount, long paidBy, string splitType, List<long> members, List<decimal>? customAmounts, string createdBy)
            => _repository.AddExpenseAsync(groupId, description, amount, paidBy, splitType, members, customAmounts, createdBy);

        public Task<OperationResult<bool>> EditExpenseAsync(long expenseId, string description, decimal amount, long paidBy, string splitType, List<long> members, List<decimal>? customAmounts, string modifiedBy)
            => _repository.EditExpenseAsync(expenseId, description, amount, paidBy, splitType, members, customAmounts, modifiedBy);

        public Task<OperationResult<bool>> DeleteExpenseAsync(long expenseId, string modifiedBy)
            => _repository.DeleteExpenseAsync(expenseId, modifiedBy);

        public Task<OperationResult<List<ExpenseResponse>>> GetExpensesByGroupAsync(long groupId)
            => _repository.GetExpensesByGroupAsync(groupId);

        public Task<OperationResult<long>> AddPersonalExpenseAsync(string description, decimal amount, long memberId, string createdBy)
            => _repository.AddPersonalExpenseAsync(description, amount, memberId, createdBy);

        public Task<OperationResult<List<ExpenseResponse>>> GetPersonalExpensesAsync(long memberId)
            => _repository.GetPersonalExpensesAsync(memberId);
    }
}
