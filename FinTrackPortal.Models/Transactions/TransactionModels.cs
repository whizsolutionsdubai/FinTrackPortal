namespace FinTrackPortal.Models;

public class TransactionHistoryItem
{
    public long RefId { get; set; }
    public string EntryType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime OccurredAt { get; set; }
    public long? GroupId { get; set; }
    public string? GroupName { get; set; }
}

public class TransactionSummaryResponse
{
    public int ExpenseEntryCount { get; set; }
    public int SettlementEntryCount { get; set; }
    public int TotalEntries { get; set; }
}
