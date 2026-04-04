namespace FinTrackPortal.Models;

/// <summary>
/// Shared Dapper row for stored procedures that return <c>SELECT @@ROWCOUNT AS RowsUpdated</c>
/// (e.g. mark-read, profile photo URL update, password hash update).
/// </summary>
public sealed class SpRowsUpdatedRow
{
    public int RowsUpdated { get; set; }
}
