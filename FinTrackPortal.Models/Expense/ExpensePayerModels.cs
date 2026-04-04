using System.ComponentModel.DataAnnotations;

namespace FinTrackPortal.Models
{
    public class AddExpensePayerRequest
    {
        [Required]
        public long ExpenseId { get; set; }

        [Required]
        public long MemberId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal AmountPaid { get; set; }
    }

    public class ExpensePayerResponse
    {
        public long PayerId { get; set; }
        public long MemberId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public decimal AmountPaid { get; set; }
    }
}
