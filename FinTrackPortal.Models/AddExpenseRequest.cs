using System.ComponentModel.DataAnnotations;

namespace FinTrackPortal.Models
{
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

        [Required]
        public long PaidBy { get; set; }

        [Required]
        [MinLength(1)]
        public List<long> Members { get; set; } = new();
    }
}
