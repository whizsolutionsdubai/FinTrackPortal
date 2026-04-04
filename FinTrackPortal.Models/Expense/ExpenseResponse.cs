namespace FinTrackPortal.Models
{
    /// <summary>
    /// Returned by sp_GetExpensesByGroup and sp_GetPersonalExpenses.
    /// Includes the payer's name resolved via JOIN.
    /// </summary>
    public class ExpenseResponse
    {
        public long ExpenseId { get; set; }
        public long? GroupId { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public long PaidBy { get; set; }
        public string PaidByName { get; set; } = string.Empty;
        public string SplitType { get; set; } = string.Empty;
        public DateTime ExpenseDate { get; set; }
        public string? ExpenseCategory { get; set; }
        public string? ForReference { get; set; }
        public string? AccountName { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
