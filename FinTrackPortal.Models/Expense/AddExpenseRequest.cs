using System.ComponentModel.DataAnnotations;

namespace FinTrackPortal.Models
{
    /// <summary>
    /// Request body for POST /api/Expense/add.
    /// SplitType is "Equal" or "Custom". When "Custom", provide CustomAmounts
    /// matching the Members list (must sum to Amount).
    /// </summary>
    public class AddExpenseRequest
    {
        [Required]
        public long GroupId { get; set; }

        [Required]
        [StringLength(250)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        [StringLength(3)]
        public string CurrencyCode { get; set; } = "AED";

        public long? EventId { get; set; }
        public long? ChecklistItemId { get; set; }
        public decimal? AmountOriginal { get; set; }

        [Required]
        public long PaidBy { get; set; }

        [Required]
        [StringLength(10)]
        public string SplitType { get; set; } = "Equal";

        [Required]
        [MinLength(1)]
        public List<long> Members { get; set; } = new();

        public List<decimal>? CustomAmounts { get; set; }
    }
}
