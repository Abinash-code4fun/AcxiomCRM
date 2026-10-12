using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.Models.ViewModels;

public class OpportunityListViewModel
{
    public List<Opportunity> Opportunities { get; set; } = new();
    public string? Search { get; set; }
    public OpportunityStage? Stage { get; set; }
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalCount { get; set; }
    public decimal TotalPipeline { get; set; }
    public decimal WeightedPipeline { get; set; }
}

public class OpportunityFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Opportunity / deal name is required.")]
    [StringLength(200, ErrorMessage = "Opportunity name cannot exceed 200 characters.")]
    [Display(Name = "Opportunity / Deal Name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select an existing customer.")]
    [Display(Name = "Customer / Account")]
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "Please assign a deal owner.")]
    [Display(Name = "Deal Owner (Sales Executive)")]
    public string OwnerId { get; set; } = string.Empty;

    [Display(Name = "Pipeline Stage")]
    public OpportunityStage Stage { get; set; } = OpportunityStage.Qualification;

    [Required(ErrorMessage = "Deal amount is required.")]
    [Range(0.01, 999999999.99, ErrorMessage = "Amount must be greater than zero.")]
    [Display(Name = "Deal Amount ($)")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Probability is required.")]
    [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100%.")]
    [Display(Name = "Win Probability (%)")]
    public int Probability { get; set; } = 25;

    [Required(ErrorMessage = "Expected close date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Expected Close Date")]
    public DateTime ExpectedCloseDate { get; set; } = DateTime.UtcNow.AddDays(30);

    [StringLength(100)]
    [Display(Name = "Deal Source")]
    public string? Source { get; set; }

    [StringLength(2000)]
    [Display(Name = "Deal Notes / Strategy")]
    public string? Notes { get; set; }

    public IEnumerable<SelectListItem> Customers { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> Owners { get; set; } = Enumerable.Empty<SelectListItem>();
}

public class OpportunityDetailsViewModel
{
    public Opportunity Opportunity { get; set; } = null!;
    public List<FollowUp> FollowUps { get; set; } = new();
}
