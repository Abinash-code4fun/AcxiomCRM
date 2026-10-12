using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models;

public class Lead
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Phone, StringLength(20)]
    public string? Phone { get; set; }

    [StringLength(200)]
    public string? Company { get; set; }

    public LeadSource Source { get; set; } = LeadSource.Web;
    public LeadStatus Status { get; set; } = LeadStatus.New;
    public LeadPriority Priority { get; set; } = LeadPriority.Medium;

    [StringLength(2000)]
    public string? Notes { get; set; }

    // Assignment
    public string? AssignedToId { get; set; }
    public string? CreatedById { get; set; }

    // Timestamps
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedAtUtc { get; set; }

    // Conversion tracking
    public int? ConvertedCustomerId { get; set; }
    public int? ConvertedOpportunityId { get; set; }
    public DateTime? ConvertedAtUtc { get; set; }

    // Navigation properties
    [NotMapped]
    public string FullName => $"{FirstName} {LastName}";

    [ForeignKey(nameof(AssignedToId))]
    public ApplicationUser? AssignedTo { get; set; }

    [ForeignKey(nameof(CreatedById))]
    public ApplicationUser? CreatedBy { get; set; }

    [ForeignKey(nameof(ConvertedCustomerId))]
    public Customer? ConvertedCustomer { get; set; }

    [ForeignKey(nameof(ConvertedOpportunityId))]
    public Opportunity? ConvertedOpportunity { get; set; }

    public ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
}
