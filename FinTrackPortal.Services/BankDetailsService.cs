using System.Text.RegularExpressions;
using FinTrackPortal.Common;
using FinTrackPortal.Interfaces;
using FinTrackPortal.Models;
using Microsoft.Extensions.Logging;

namespace FinTrackPortal.Services;

public sealed class BankDetailsService : IBankDetailsService
{
    private readonly IBankDetailsRepository _repository;
    private readonly IEncryptionService _encryption;
    private readonly ILogger<BankDetailsService> _logger;

    public BankDetailsService(
        IBankDetailsRepository repository,
        IEncryptionService encryption,
        ILogger<BankDetailsService> logger)
    {
        _repository = repository;
        _encryption = encryption;
        _logger = logger;
    }

    public async Task<OperationResult<BankDetailsResponse?>> GetAsync(long memberId)
    {
        var row = await _repository.GetForMemberAsync(memberId);
        return OperationResult<BankDetailsResponse?>.Success(row);
    }

    public async Task<OperationResult<bool>> SaveAsync(long memberId, SaveBankDetailsRequest request)
    {
        if (!_encryption.IsConfigured)
            return OperationResult<bool>.Failure("Bank encryption is not configured on the server (Encryption:Key / Encryption:IV).");

        var normalized = NormalizeIban(request.IBAN);
        if (!IsValidIbanFormat(normalized))
            return OperationResult<bool>.Failure("Invalid IBAN format.");

        try
        {
            var enc = _encryption.Encrypt(normalized);
            var masked = MaskIban(normalized);
            await _repository.SaveAsync(memberId, request.BankName.Trim(), request.AccountHolderName.Trim(), enc, masked);
            return OperationResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Save bank details failed");
            return OperationResult<bool>.Failure("Could not save bank details.");
        }
    }

    public async Task<OperationResult<bool>> DeleteAsync(long memberId)
    {
        var n = await _repository.DeleteForMemberAsync(memberId);
        return n > 0
            ? OperationResult<bool>.Success(true)
            : OperationResult<bool>.Failure("No bank details to delete.");
    }

    public async Task<OperationResult<PayeeBankDetailsForSettlementResponse>> GetPayeeBankForSettlementAsync(
        decimal settlementId, long requestingMemberId)
    {
        if (!_encryption.IsConfigured)
            return OperationResult<PayeeBankDetailsForSettlementResponse>.Failure("Bank encryption is not configured.");

        var row = await _repository.GetPayeeBankForSettlementAsync(settlementId, requestingMemberId);
        if (row == null)
            return OperationResult<PayeeBankDetailsForSettlementResponse>.Failure("Forbidden or settlement not found.");

        try
        {
            var plain = _encryption.Decrypt(row.EncryptedIBAN);
            return OperationResult<PayeeBankDetailsForSettlementResponse>.Success(new PayeeBankDetailsForSettlementResponse
            {
                BankName = row.BankName,
                AccountHolderName = row.AccountHolderName,
                IbanPlain = plain
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Decrypt IBAN for settlement");
            return OperationResult<PayeeBankDetailsForSettlementResponse>.Failure("Could not read bank details.");
        }
    }

    private static string NormalizeIban(string iban) =>
        Regex.Replace(iban.Trim(), @"\s+", "").ToUpperInvariant();

    private static bool IsValidIbanFormat(string iban)
    {
        if (iban.Length is < 15 or > 34)
            return false;
        return Regex.IsMatch(iban, @"^[A-Z]{2}[0-9]{2}[A-Z0-9]+$");
    }

    private static string MaskIban(string plain)
    {
        if (plain.Length <= 8)
            return "****";
        var start = plain[..4];
        var end = plain[^4..];
        return $"{start} **** **** **** {end}";
    }
}
