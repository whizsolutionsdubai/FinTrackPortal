using FinTrackPortal.Common;
using FinTrackPortal.Models;

namespace FinTrackPortal.Interfaces
{
    /// <summary>
    /// Data-access contract for settlement (debt payment) operations.
    /// Implemented by SettlementRepository using Dapper + stored procedures.
    /// </summary>
    public interface ISettlementRepository
    {
        /// <summary>Record a payment from one member to another (sp_RecordSettlement). Returns SettlementId.</summary>
        Task<OperationResult<long>> RecordSettlementAsync(
            long groupId, long fromMemberId, long toMemberId,
            decimal amount, string createdBy);

        /// <summary>Get all settlement records for a group (sp_GetSettlementsByGroup).</summary>
        Task<OperationResult<List<SettlementResponse>>> GetSettlementsByGroupAsync(long groupId);
    }
}
