using FinTrackPortal.Models;

namespace FinTrackPortal.Interfaces;

public interface IBankDetailsRepository
{
    Task<BankDetailsResponse?> GetForMemberAsync(long memberId);

    Task SaveAsync(long memberId, string bankName, string accountHolderName, string encryptedIban, string maskedIban);

    Task<int> DeleteForMemberAsync(long memberId);

    Task<PayeeBankEncryptedRow?> GetPayeeBankForSettlementAsync(decimal settlementId, long requestingMemberId);
}
