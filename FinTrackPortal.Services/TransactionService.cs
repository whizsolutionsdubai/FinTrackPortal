using FinTrackPortal.Common;
using FinTrackPortal.Interfaces;
using FinTrackPortal.Models;

namespace FinTrackPortal.Services;

public sealed class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _repository;

    public TransactionService(ITransactionRepository repository)
    {
        _repository = repository;
    }

    public async Task<OperationResult<(IReadOnlyList<TransactionHistoryItem> Items, TransactionSummaryResponse? Summary)>> GetHistoryWithSummaryAsync(
        long memberId, int skip, int take)
    {
        if (take > 200) take = 200;
        if (skip < 0) skip = 0;

        var items = await _repository.GetHistoryAsync(memberId, skip, take);
        var summary = await _repository.GetSummaryAsync(memberId);
        return OperationResult<(IReadOnlyList<TransactionHistoryItem>, TransactionSummaryResponse?)>.Success((items, summary));
    }
}
