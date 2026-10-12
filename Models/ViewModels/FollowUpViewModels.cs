using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.Models.ViewModels;

public class FollowUpListViewModel
{
    public List<FollowUp> FollowUps { get; set; } = new();
    public List<FollowUp> Upcoming { get; set; } = new();
    public List<FollowUp> Overdue { get; set; } = new();
    public FollowUpStatus? Status { get; set; }
    public FollowUpType? Type { get; set; }
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalCount { get; set; }
}

public class FollowUpFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Subject is required.")]
    [StringLength(200, ErrorMessage = "Subject cannot exceed 200 characters.")]
    [Display(Name = "Subject / Title")]
    public string Subject { get; set; } = string.Empty;

    [Display(Name = "Activity Type")]
    public FollowUpType Type { get; set; } = FollowUpType.Call;

    [Required(ErrorMessage = "Assigned user is required.")]
    [Display(Name = "Assigned To")]
    public string AssignedToId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Scheduled date and time is required.")]
    [Display(Name = "Scheduled Date & Time")]
    public DateTime ScheduledDate { get; set; } = DateTime.UtcNow.AddDays(1);

    [Display(Name = "Status")]
    public FollowUpStatus Status { get; set; } = FollowUpStatus.Planned;

    [Display(Name = "Related Customer (Optional)")]
    public int? CustomerId { get; set; }

    [Display(Name = "Related Lead (Optional)")]
    public int? LeadId { get; set; }

    [Display(Name = "Related Opportunity (Optional)")]
    public int? OpportunityId { get; set; }

    [StringLength(2000)]
    [Display(Name = "Activity Notes / Agenda")]
    public string? Notes { get; set; }

    public IEnumerable<SelectListItem> Users { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> Customers { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> Leads { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> Opportunities { get; set; } = Enumerable.Empty<SelectListItem>();
}

public class CompleteFollowUpViewModel
{
    public int Id { get; set; }
    public string Subject { get; set; } = string.Empty;

    [Display(Name = "Completion Outcome / Notes")]
    [StringLength(2000)]
    public string? Notes { get; set; }
}

public class RescheduleFollowUpViewModel
{
    public int Id { get; set; }
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "New date and time is required.")]
    [Display(Name = "New Scheduled Date & Time")]
    public DateTime NewDate { get; set; } = DateTime.UtcNow.AddDays(1);

    [Display(Name = "Reschedule Reason / Notes")]
    [StringLength(2000)]
    public string? Notes { get; set; }
}
