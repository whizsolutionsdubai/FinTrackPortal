using System.ComponentModel.DataAnnotations;

namespace FinTrackPortal.Models
{
    /// <summary>
    /// Request body for PUT /api/Expense/edit.
    /// Replaces the expense details and rebuilds all splits in a single transaction.
    /// </summary>
    public class EditExpenseRequest
    {
        [Required]
        public long ExpenseId { get; set; }

        [Required]
        [StringLength(250)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        [Required]
        public long PaidBy { get; set; }

        [Required]
        [StringLength(10)]
        public string SplitType { get; set; } = "Equal";

        [Required]
        [MinLength(1)]
        public List<long> Members { get; set; } = new();

        public List<decimal>? CustomAmounts { get; set; }

        [StringLength(20)]
        public string? ExpenseCategory { get; set; }

        [StringLength(200)]
        public string? ForReference { get; set; }
    }
}
