namespace FinTrackPortal.Models
{
    public class ExpenseResponse
    {
        public long ExpenseId { get; set; }
        public long? GroupId { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public long PaidBy { get; set; }
        public string PaidByName { get; set; } = string.Empty;
        public string SplitType { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }
}
