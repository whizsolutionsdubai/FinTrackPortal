using FinTrackPortal.Common;
using FinTrackPortal.Models;

namespace FinTrackPortal.Services;

public interface ITransactionService
{
    Task<OperationResult<(IReadOnlyList<TransactionHistoryItem> Items, TransactionSummaryResponse? Summary)>> GetHistoryWithSummaryAsync(
        long memberId, int skip, int take);
}
