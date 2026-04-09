using System.ComponentModel.DataAnnotations;

namespace FinTrackPortal.Models;

public class AddChecklistItemRequest
{
    [Required]
    [StringLength(300)]
    public string ItemName { get; set; } = string.Empty;
    public int QuantityNeeded { get; set; } = 1;
    public string? QuantityUnit { get; set; }
    public long? SuggestedMemberId { get; set; }
    public bool IsMandatory { get; set; } = false;
    public long? AssignedMemberId { get; set; }
}

public class UpdateChecklistItemRequest
{
    public string? ItemName { get; set; }
    public int? QuantityNeeded { get; set; }
    public string? QuantityUnit { get; set; }
    public long? SuggestedMemberId { get; set; }
    public bool? IsMandatory { get; set; }
    public long? AssignedMemberId { get; set; }
    public string? Status { get; set; }
}

public class ClaimChecklistItemRequest
{
    public int QuantityClaimed { get; set; } = 1;
}

public class ChecklistClaimSummary
{
    public long MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public int QuantityClaimed { get; set; }
    public DateTime ClaimedDate { get; set; }
}

public class EventChecklistItemResponse
{
    public long ChecklistItemId { get; set; }
    public long EventId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int QuantityNeeded { get; set; }
    public string? QuantityUnit { get; set; }
    public int TotalQuantityClaimed { get; set; }
    public int QuantityRemaining { get; set; }
    public long? SuggestedMemberId { get; set; }
    public string? SuggestedMemberName { get; set; }
    public string Status { get; set; } = "Open";
    public bool IsMandatory { get; set; }
    public long? AssignedMemberId { get; set; }
    public string? AssignedMemberName { get; set; }
    public bool IsAssigned => AssignedMemberId.HasValue;
    public bool IsClaimedByMe { get; set; }
    public int MyClaimedQuantity { get; set; }
    public List<ChecklistClaimSummary> Claims { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}
