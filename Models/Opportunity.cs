using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models;

public class Opportunity
{
    public int Id { get; set; }

    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    // Related customer
    public int CustomerId { get; set; }

    // Owner (sales exec)
    [Required]
    public string OwnerId { get; set; } = string.Empty;

    public OpportunityStage Stage { get; set; } = OpportunityStage.Qualification;

    [Column(TypeName = "decimal(18,2)")]
    [Range(0.01, 999999999.99, ErrorMessage = "Amount must be greater than zero.")]
    public decimal Amount { get; set; }

    [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
    public int Probability { get; set; }

    [DataType(DataType.Date)]
    public DateTime ExpectedCloseDate { get; set; }

    [StringLength(100)]
    public string? Source { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }

    public string? CreatedById { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedAtUtc { get; set; }

    // Computed property
    [NotMapped]
    public decimal WeightedValue => Amount * Probability / 100m;

    // Navigation properties
    [ForeignKey(nameof(CustomerId))]
    public Customer? Customer { get; set; }

    [ForeignKey(nameof(OwnerId))]
    public ApplicationUser? Owner { get; set; }

    [ForeignKey(nameof(CreatedById))]
    public ApplicationUser? CreatedBy { get; set; }

    public ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
}
