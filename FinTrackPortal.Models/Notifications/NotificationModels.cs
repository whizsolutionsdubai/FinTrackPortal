namespace FinTrackPortal.Models;

public class NotificationItem
{
    public long NotificationId { get; set; }
    public long MemberId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Body { get; set; }
    public string? NotificationType { get; set; }
    public bool IsRead { get; set; }
    public string? LinkUrl { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
}
