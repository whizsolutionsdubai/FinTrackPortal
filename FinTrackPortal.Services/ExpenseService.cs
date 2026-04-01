using FinTrackPortal.Common;
using FinTrackPortal.Interfaces;

namespace FinTrackPortal.Services
{
    public class ExpenseService : IExpenseService
    {
        private readonly IExpenseRepository _repository;

        public ExpenseService(IExpenseRepository repository)
        {
            _repository = repository;
        }

        public Task<OperationResult<long>> AddExpenseAsync(long groupId, string description, decimal amount, long paidBy, List<long> members, string createdBy)
            => _repository.AddExpenseAsync(groupId, description, amount, paidBy, members, createdBy);

        public Task<OperationResult<bool>> EditExpenseAsync(long expenseId, string description, decimal amount, long paidBy, List<long> members, string modifiedBy)
            => _repository.EditExpenseAsync(expenseId, description, amount, paidBy, members, modifiedBy);

        public Task<OperationResult<bool>> DeleteExpenseAsync(long expenseId, string modifiedBy)
            => _repository.DeleteExpenseAsync(expenseId, modifiedBy);
    }
}
