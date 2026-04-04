namespace FinTrackPortal.Models;

/// <summary>Row for payer-only settlement view — ciphertext for server-side decrypt.</summary>
public class PayeeBankEncryptedRow
{
    public long BankDetailId { get; set; }
    public long MemberId { get; set; }
    public string BankName { get; set; } = string.Empty;
    public string AccountHolderName { get; set; } = string.Empty;
    public string EncryptedIBAN { get; set; } = string.Empty;
}
