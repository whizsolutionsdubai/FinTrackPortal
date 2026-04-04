using System.ComponentModel.DataAnnotations;

namespace FinTrackPortal.Models
{
    /// <summary>
    /// Request body for POST /api/Expense/move.
    /// Moves an expense from one group to another by updating its GroupId.
    /// Expense splits remain unchanged — edit the expense after moving if needed.
    /// </summary>
    public class MoveExpenseRequest
    {
        [Required]
        public long ExpenseId { get; set; }

        [Required]
        public long NewGroupId { get; set; }
    }
}
