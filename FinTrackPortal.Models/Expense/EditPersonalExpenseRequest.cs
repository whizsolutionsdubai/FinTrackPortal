using System.ComponentModel.DataAnnotations;

namespace FinTrackPortal.Models
{
    /// <summary>Request body for PUT /api/Expense/personal/edit.</summary>
    public class EditPersonalExpenseRequest
    {
        [Required]
        public long ExpenseId { get; set; }

        [Required]
        [StringLength(250)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        [StringLength(3)]
        public string? CurrencyCode { get; set; }

        public DateTime? ExpenseDate { get; set; }

        public long? AccountId { get; set; }

        [StringLength(20)]
        public string? ExpenseCategory { get; set; }

        [StringLength(200)]
        public string? ForReference { get; set; }
    }
}
