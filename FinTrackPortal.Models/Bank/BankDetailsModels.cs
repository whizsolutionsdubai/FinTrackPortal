using System.ComponentModel.DataAnnotations;

namespace FinTrackPortal.Models;

public class BankDetailsResponse
{
    public long BankDetailId { get; set; }
    public long MemberId { get; set; }
    public string BankName { get; set; } = string.Empty;
    public string AccountHolderName { get; set; } = string.Empty;
    public string MaskedIBAN { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class SaveBankDetailsRequest
{
    [Required]
    [StringLength(100)]
    public string BankName { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string AccountHolderName { get; set; } = string.Empty;

    /// <summary>Plain IBAN; validated server-side before encryption.</summary>
    [Required]
    [StringLength(34, MinimumLength = 8)]
    public string IBAN { get; set; } = string.Empty;
}

public class PayeeBankDetailsForSettlementResponse
{
    public string BankName { get; set; } = string.Empty;
    public string AccountHolderName { get; set; } = string.Empty;
    public string IbanPlain { get; set; } = string.Empty;
}
