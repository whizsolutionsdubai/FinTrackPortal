using FinTrackPortal.Models;

namespace FinTrackPortal.Interfaces;

public interface ITransactionRepository
{
    Task<IReadOnlyList<TransactionHistoryItem>> GetHistoryAsync(long memberId, int skip, int take);

    Task<TransactionSummaryResponse?> GetSummaryAsync(long memberId);
}
