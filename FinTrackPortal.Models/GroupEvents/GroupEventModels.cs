using System.ComponentModel.DataAnnotations;

namespace FinTrackPortal.Models;

public class GroupEventResponse
{
    public long EventId { get; set; }
    public long GroupId { get; set; }
    public long CreatedByMemberId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    public string? Location { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateGroupEventRequest
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    public DateTime EventDate { get; set; }

    [StringLength(300)]
    public string? Location { get; set; }
}

public class UpdateGroupEventRequest
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    public DateTime EventDate { get; set; }

    [StringLength(300)]
    public string? Location { get; set; }
}
