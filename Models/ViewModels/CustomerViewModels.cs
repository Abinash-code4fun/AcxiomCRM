using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.Models.ViewModels;

public class CustomerListViewModel
{
    public List<Customer> Customers { get; set; } = new();
    public string? Search { get; set; }
    public CustomerStatus? Status { get; set; }
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalCount { get; set; }
}

public class CustomerFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Customer name is required.")]
    [StringLength(150, ErrorMessage = "Name cannot exceed 150 characters.")]
    [Display(Name = "Company / Customer Name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [StringLength(256)]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Please enter a valid phone number.")]
    [StringLength(20, MinimumLength = 7, ErrorMessage = "Phone number should be between 7 and 20 digits.")]
    [RegularExpression(@"^[\+]?[(]?[0-9]{3}[)]?[-\s\.]?[0-9]{3}[-\s\.]?[0-9]{4,6}$", ErrorMessage = "Please enter a valid phone number format (e.g. +1-555-555-5555 or 555-555-5555).")]
    [Display(Name = "Phone Number")]
    public string? Phone { get; set; }

    [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters.")]
    [Display(Name = "Billing / Physical Address")]
    public string? Address { get; set; }

    [Display(Name = "Account Status")]
    public CustomerStatus Status { get; set; } = CustomerStatus.Active;

    [StringLength(2000)]
    [Display(Name = "Notes / Description")]
    public string? Notes { get; set; }

    [Display(Name = "Assigned Sales Executive")]
    public string? AssignedToId { get; set; }

    public IEnumerable<SelectListItem> SalesExecutives { get; set; } = Enumerable.Empty<SelectListItem>();
}

public class CustomerDetailsViewModel
{
    public Customer Customer { get; set; } = null!;
    public List<Opportunity> Opportunities { get; set; } = new();
    public List<FollowUp> FollowUps { get; set; } = new();
}
