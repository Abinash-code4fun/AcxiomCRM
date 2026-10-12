using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models;

public class FollowUp
{
    public int Id { get; set; }

    [Required, StringLength(200)]
    public string Subject { get; set; } = string.Empty;

    public FollowUpType Type { get; set; } = FollowUpType.Call;

    // Polymorphic relationship
    public RelatedEntityType? RelatedEntityType { get; set; }
    public int? RelatedEntityId { get; set; }

    // Direct FK relationships (nullable for flexibility)
    public int? CustomerId { get; set; }
    public int? LeadId { get; set; }
    public int? OpportunityId { get; set; }

    [Required]
    public string AssignedToId { get; set; } = string.Empty;

    public DateTime ScheduledDate { get; set; }

    public FollowUpStatus Status { get; set; } = FollowUpStatus.Planned;

    [StringLength(2000)]
    public string? Notes { get; set; }

    public string? CreatedById { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }

    // Navigation properties
    [ForeignKey(nameof(AssignedToId))]
    public ApplicationUser? AssignedTo { get; set; }

    [ForeignKey(nameof(CreatedById))]
    public ApplicationUser? CreatedBy { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public Customer? Customer { get; set; }

    [ForeignKey(nameof(LeadId))]
    public Lead? Lead { get; set; }

    [ForeignKey(nameof(OpportunityId))]
    public Opportunity? Opportunity { get; set; }
}
