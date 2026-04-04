using FinTrackPortal.Common;
using FinTrackPortal.Models;

namespace FinTrackPortal.Services;

public interface IBankDetailsService
{
    Task<OperationResult<BankDetailsResponse?>> GetAsync(long memberId);

    Task<OperationResult<bool>> SaveAsync(long memberId, SaveBankDetailsRequest request);

    Task<OperationResult<bool>> DeleteAsync(long memberId);

    Task<OperationResult<PayeeBankDetailsForSettlementResponse>> GetPayeeBankForSettlementAsync(decimal settlementId, long requestingMemberId);
}
