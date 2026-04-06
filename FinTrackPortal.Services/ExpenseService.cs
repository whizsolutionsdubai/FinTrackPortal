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

        public Task<OperationResult<long>> AddExpenseAsync(long groupId, string description, decimal amount, string currencyCode, long paidBy, string splitType, List<long> members, List<decimal>? customAmounts, string createdBy)
            => _repository.AddExpenseAsync(groupId, description, amount, currencyCode, paidBy, splitType, members, customAmounts, createdBy);

        public Task<OperationResult<bool>> EditExpenseAsync(long expenseId, string description, decimal amount, string? currencyCode, long paidBy, string splitType, List<long> members, List<decimal>? customAmounts, string modifiedBy, string? expenseCategory, string? forReference)
            => _repository.EditExpenseAsync(expenseId, description, amount, currencyCode, paidBy, splitType, members, customAmounts, modifiedBy, expenseCategory, forReference);

        public Task<OperationResult<bool>> DeleteExpenseAsync(long expenseId, string modifiedBy)
            => _repository.DeleteExpenseAsync(expenseId, modifiedBy);

        public Task<OperationResult<List<ExpenseResponse>>> GetExpensesByGroupAsync(long groupId)
            => _repository.GetExpensesByGroupAsync(groupId);

        public Task<OperationResult<long>> AddPersonalExpenseAsync(string description, decimal amount, string currencyCode, long memberId, string createdBy, DateTime? expenseDate, long? accountId, string? expenseCategory, string? forReference)
            => _repository.AddPersonalExpenseAsync(description, amount, currencyCode, memberId, createdBy, expenseDate, accountId, expenseCategory, forReference);

        public Task<OperationResult<bool>> UpdatePersonalExpenseAsync(long expenseId, long memberId, string description, decimal amount, string? currencyCode, DateTime? expenseDate, long? accountId, string? expenseCategory, string? forReference, string modifiedBy)
            => _repository.UpdatePersonalExpenseAsync(expenseId, memberId, description, amount, currencyCode, expenseDate, accountId, expenseCategory, forReference, modifiedBy);

        public Task<OperationResult<List<ExpenseResponse>>> GetPersonalExpensesAsync(long memberId, string? category)
            => _repository.GetPersonalExpensesAsync(memberId, category);

        public Task<OperationResult<bool>> MoveExpenseAsync(long expenseId, long newGroupId, string modifiedBy)
            => _repository.MoveExpenseAsync(expenseId, newGroupId, modifiedBy);

        public Task<OperationResult<List<ExpenseAccountResponse>>> GetUserAccountsAsync(long userId)
            => _repository.GetUserAccountsAsync(userId);

        public Task<OperationResult<long>> CreateAccountAsync(long userId, string accountName, string? accountColor)
            => _repository.CreateAccountAsync(userId, accountName, accountColor);

        public Task<OperationResult<bool>> DeleteAccountAsync(long accountId)
            => _repository.DeleteAccountAsync(accountId);

        public Task<OperationResult<long>> AddAttachmentAsync(long expenseId, string fileName, string fileUrl, string fileType, int? fileSizeKB, string uploadedBy)
            => _repository.AddAttachmentAsync(expenseId, fileName, fileUrl, fileType, fileSizeKB, uploadedBy);

        public Task<OperationResult<List<AttachmentResponse>>> GetAttachmentsAsync(long expenseId)
            => _repository.GetAttachmentsAsync(expenseId);

        public Task<OperationResult<List<MemberAttachmentResponse>>> GetAllAttachmentsAsync(long memberId)
            => _repository.GetAllAttachmentsAsync(memberId);

        public Task<OperationResult<bool>> DeleteAttachmentAsync(long attachmentId)
            => _repository.DeleteAttachmentAsync(attachmentId);

        public Task<OperationResult<long>> AddExpensePayerAsync(AddExpensePayerRequest request)
            => _repository.AddExpensePayerAsync(request);

        public Task<OperationResult<List<ExpensePayerResponse>>> GetExpensePayersAsync(long expenseId)
            => _repository.GetExpensePayersAsync(expenseId);
    }
}
