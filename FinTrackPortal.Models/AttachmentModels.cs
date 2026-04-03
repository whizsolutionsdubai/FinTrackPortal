namespace FinTrackPortal.Models
{
    /// <summary>Response model for expense attachment metadata.</summary>
    public class AttachmentResponse
    {
        public long AttachmentId { get; set; }
        public long ExpenseId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public int? FileSizeKB { get; set; }
        public DateTime UploadedDate { get; set; }
    }

    /// <summary>Attachment row for &quot;my receipts&quot; with linked expense context.</summary>
    public class MemberAttachmentResponse
    {
        public long AttachmentId { get; set; }
        public long ExpenseId { get; set; }
        public string ExpenseDescription { get; set; } = string.Empty;
        public decimal ExpenseAmount { get; set; }
        public DateTime ExpenseDate { get; set; }
        public string? GroupName { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public int? FileSizeKB { get; set; }
        public DateTime UploadedDate { get; set; }
    }
}
