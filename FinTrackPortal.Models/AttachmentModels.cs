namespace FinTrackPortal.Models
{
    /// <summary>Response model for expense attachment metadata.</summary>
    public class AttachmentResponse
    {
        public long AttachmentId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public int? FileSizeKB { get; set; }
        public DateTime UploadedDate { get; set; }
    }
}
