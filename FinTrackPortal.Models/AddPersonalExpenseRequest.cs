using System.ComponentModel.DataAnnotations;

namespace FinTrackPortal.Models
{
    /// <summary>
    /// Request body for POST /api/Expense/personal.
    /// Personal expenses have GroupId = NULL in the database.
    /// </summary>
    public class AddPersonalExpenseRequest
    {
        [Required]
        [StringLength(250)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }
    }
}
