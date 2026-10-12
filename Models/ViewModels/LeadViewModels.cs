using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.Models.ViewModels;

public class LeadListViewModel
{
    public List<Lead> Leads { get; set; } = new();
    public string? Search { get; set; }
    public LeadStatus? Status { get; set; }
    public LeadPriority? Priority { get; set; }
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalCount { get; set; }
}

public class LeadFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "First name is required.")]
    [StringLength(100, ErrorMessage = "First name cannot exceed 100 characters.")]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(100, ErrorMessage = "Last name cannot exceed 100 characters.")]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [StringLength(256)]
    [Display(Name = "Work Email")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Please enter a valid phone number.")]
    [StringLength(20, MinimumLength = 7, ErrorMessage = "Phone number should be between 7 and 20 digits.")]
    [RegularExpression(@"^[\+]?[(]?[0-9]{3}[)]?[-\s\.]?[0-9]{3}[-\s\.]?[0-9]{4,6}$", ErrorMessage = "Please enter a valid phone number format (e.g. +1-555-555-5555 or 555-555-5555).")]
    [Display(Name = "Phone Number")]
    public string? Phone { get; set; }

    [StringLength(200, ErrorMessage = "Company cannot exceed 200 characters.")]
    [Display(Name = "Organization / Company")]
    public string? Company { get; set; }

    [Display(Name = "Lead Source")]
    public LeadSource Source { get; set; } = LeadSource.Web;

    [Display(Name = "Current Status")]
    public LeadStatus Status { get; set; } = LeadStatus.New;

    [Display(Name = "Priority Level")]
    public LeadPriority Priority { get; set; } = LeadPriority.Medium;

    [StringLength(2000)]
    [Display(Name = "Internal Notes")]
    public string? Notes { get; set; }

    [Display(Name = "Assign To")]
    public string? AssignedToId { get; set; }

    public IEnumerable<SelectListItem> SalesExecutives { get; set; } = Enumerable.Empty<SelectListItem>();
}

public class LeadConvertViewModel
{
    public int LeadId { get; set; }
    public string LeadName { get; set; } = string.Empty;
    public string LeadEmail { get; set; } = string.Empty;
    public string? LeadCompany { get; set; }

    [Display(Name = "Also create a deal/opportunity?")]
    public bool CreateOpportunity { get; set; } = true;

    [Display(Name = "Opportunity Name")]
    [StringLength(200)]
    public string? OpportunityName { get; set; }

    [Display(Name = "Estimated Deal Amount ($)")]
    [Range(0.01, 999999999.99, ErrorMessage = "Amount must be greater than zero.")]
    public decimal? OpportunityAmount { get; set; } = 5000m;

    [Display(Name = "Expected Close Date")]
    [DataType(DataType.Date)]
    public DateTime? ExpectedCloseDate { get; set; } = DateTime.UtcNow.AddDays(30);

    [Display(Name = "Opportunity Notes")]
    [StringLength(2000)]
    public string? Notes { get; set; }
}

public class LeadDetailsViewModel
{
    public Lead Lead { get; set; } = null!;
    public List<FollowUp> FollowUps { get; set; } = new();
}
