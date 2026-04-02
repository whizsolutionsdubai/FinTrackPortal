using System.ComponentModel.DataAnnotations;

namespace FinTrackPortal.Models
{
    /// <summary>
    /// Request body for POST /api/Expense/accounts.
    /// Creates a user-defined expense account label (e.g. "Personal", "Customer - NMC").
    /// </summary>
    public class CreateAccountRequest
    {
        [Required]
        public long UserId { get; set; }

        [Required]
        [StringLength(100)]
        public string AccountName { get; set; } = string.Empty;

        /// <summary>Optional hex colour for UI badge, e.g. "#0ABFBC".</summary>
        [StringLength(7)]
        public string? AccountColor { get; set; }
    }

    /// <summary>Response model for expense account labels.</summary>
    public class ExpenseAccountResponse
    {
        public long AccountId { get; set; }
        public string AccountName { get; set; } = string.Empty;
        public string? AccountColor { get; set; }
    }
}
