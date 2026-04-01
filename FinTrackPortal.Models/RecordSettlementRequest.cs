using System.ComponentModel.DataAnnotations;

namespace FinTrackPortal.Models
{
    /// <summary>
    /// Request body for POST /api/Settlement/record.
    /// FromMemberId is the payer (debtor), ToMemberId is the receiver (creditor).
    /// </summary>
    public class RecordSettlementRequest
    {
        [Required]
        public long GroupId { get; set; }

        [Required]
        public long FromMemberId { get; set; }

        [Required]
        public long ToMemberId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }
    }
}
