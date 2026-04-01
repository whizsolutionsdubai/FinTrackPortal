using FinTrackPortal.Common;
using FinTrackPortal.Models;

namespace FinTrackPortal.Services
{
    /// <summary>
    /// Business-logic contract for settlement operations.
    /// Record and history are pass-through to the repository.
    /// Suggested settlements are calculated in C# from group summary balances.
    /// </summary>
    public interface ISettlementService
    {
        /// <summary>Record that FromMember paid ToMember a specific amount.</summary>
        Task<OperationResult<long>> RecordSettlementAsync(
            long groupId, long fromMemberId, long toMemberId,
            decimal amount, string createdBy);

        /// <summary>Get full settlement payment history for a group.</summary>
        Task<OperationResult<List<SettlementResponse>>> GetSettlementsByGroupAsync(long groupId);

        /// <summary>
        /// Calculate optimal payments to clear all debts in a group.
        /// Uses a greedy debtor/creditor matching algorithm on the group's Net balances.
        /// </summary>
        Task<OperationResult<List<SuggestedSettlementResponse>>> GetSuggestedSettlementsAsync(long groupId);
    }
}
