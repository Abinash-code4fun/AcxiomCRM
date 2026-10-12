using System.Security.Claims;
using AcxiomCRM.Models;
using AcxiomCRM.Models.ViewModels;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

[Authorize]
public class LeadController : Controller
{
    private readonly ILeadService _leadService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<LeadController> _logger;

    public LeadController(
        ILeadService leadService,
        UserManager<ApplicationUser> userManager,
        ILogger<LeadController> logger)
    {
        _leadService = leadService;
        _userManager = userManager;
        _logger = logger;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    private string CurrentUserRole => User.IsInRole(AppRoles.Admin) ? AppRoles.Admin :
                                     User.IsInRole(AppRoles.Manager) ? AppRoles.Manager :
                                     AppRoles.SalesExecutive;

    private async Task PopulateSalesExecutivesAsync(LeadFormViewModel model)
    {
        var users = await _userManager.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.FullName ?? u.UserName)
            .Select(u => new SelectListItem
            {
                Value = u.Id,
                Text = u.FullName ?? u.UserName
            })
            .ToListAsync();

        model.SalesExecutives = users;
    }

    public async Task<IActionResult> Index(string? search, LeadStatus? status, LeadPriority? priority, int page = 1)
    {
        const int pageSize = 15;
        var leads = await _leadService.GetAllAsync(CurrentUserId, CurrentUserRole, search, status, priority, page, pageSize);
        var totalCount = await _leadService.GetCountAsync(CurrentUserId, CurrentUserRole, search, status, priority);

        var vm = new LeadListViewModel
        {
            Leads = leads,
            Search = search,
            Status = status,
            Priority = priority,
            Page = page,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };

        return View(vm);
    }

    public async Task<IActionResult> Details(int id)
    {
        var lead = await _leadService.GetByIdAsync(id, CurrentUserId, CurrentUserRole);
        if (lead == null)
        {
            TempData["ErrorMessage"] = "Lead not found or you are not authorized to view this record.";
            return RedirectToAction(nameof(Index));
        }

        var vm = new LeadDetailsViewModel
        {
            Lead = lead,
            FollowUps = lead.FollowUps.OrderByDescending(f => f.ScheduledDate).ToList()
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var vm = new LeadFormViewModel
        {
            AssignedToId = CurrentUserRole == AppRoles.SalesExecutive ? CurrentUserId : null
        };
        await PopulateSalesExecutivesAsync(vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LeadFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateSalesExecutivesAsync(model);
            return View(model);
        }

        var lead = new Lead
        {
            FirstName = model.FirstName.Trim(),
            LastName = model.LastName.Trim(),
            Email = model.Email.Trim().ToLower(),
            Phone = model.Phone?.Trim(),
            Company = model.Company?.Trim(),
            Source = model.Source,
            Status = model.Status,
            Priority = model.Priority,
            Notes = model.Notes?.Trim(),
            AssignedToId = model.AssignedToId
        };

        var (success, error, created) = await _leadService.CreateAsync(lead, CurrentUserId);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to create lead.");
            await PopulateSalesExecutivesAsync(model);
            return View(model);
        }

        TempData["SuccessMessage"] = $"Lead '{lead.FullName}' was successfully created.";
        return RedirectToAction(nameof(Details), new { id = created!.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var lead = await _leadService.GetByIdAsync(id, CurrentUserId, CurrentUserRole);
        if (lead == null)
        {
            TempData["ErrorMessage"] = "Lead not found or you do not have permission to edit it.";
            return RedirectToAction(nameof(Index));
        }

        var vm = new LeadFormViewModel
        {
            Id = lead.Id,
            FirstName = lead.FirstName,
            LastName = lead.LastName,
            Email = lead.Email,
            Phone = lead.Phone,
            Company = lead.Company,
            Source = lead.Source,
            Status = lead.Status,
            Priority = lead.Priority,
            Notes = lead.Notes,
            AssignedToId = lead.AssignedToId
        };

        await PopulateSalesExecutivesAsync(vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, LeadFormViewModel model)
    {
        if (id != model.Id)
            return BadRequest();

        if (!ModelState.IsValid)
        {
            await PopulateSalesExecutivesAsync(model);
            return View(model);
        }

        var lead = new Lead
        {
            Id = model.Id,
            FirstName = model.FirstName.Trim(),
            LastName = model.LastName.Trim(),
            Email = model.Email.Trim().ToLower(),
            Phone = model.Phone?.Trim(),
            Company = model.Company?.Trim(),
            Source = model.Source,
            Status = model.Status,
            Priority = model.Priority,
            Notes = model.Notes?.Trim(),
            AssignedToId = model.AssignedToId
        };

        var (success, error) = await _leadService.UpdateAsync(lead, CurrentUserId, CurrentUserRole);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to update lead.");
            await PopulateSalesExecutivesAsync(model);
            return View(model);
        }

        TempData["SuccessMessage"] = $"Lead '{lead.FullName}' was successfully updated.";
        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Convert(int id)
    {
        var lead = await _leadService.GetByIdAsync(id, CurrentUserId, CurrentUserRole);
        if (lead == null)
        {
            TempData["ErrorMessage"] = "Lead not found or unauthorized.";
            return RedirectToAction(nameof(Index));
        }

        if (lead.Status == LeadStatus.Converted)
        {
            TempData["WarningMessage"] = "This lead is already converted.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var vm = new LeadConvertViewModel
        {
            LeadId = lead.Id,
            LeadName = lead.FullName,
            LeadEmail = lead.Email,
            LeadCompany = lead.Company,
            OpportunityName = $"Deal: {lead.FullName} ({lead.Company ?? "Direct"})",
            OpportunityAmount = 5000m,
            ExpectedCloseDate = DateTime.UtcNow.AddDays(30)
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Convert(LeadConvertViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var req = new LeadConversionRequest(
            model.LeadId,
            model.CreateOpportunity,
            model.OpportunityName,
            model.OpportunityAmount,
            model.ExpectedCloseDate,
            model.Notes
        );

        var result = await _leadService.ConvertLeadAsync(req, CurrentUserId, CurrentUserRole);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Conversion failed.");
            return View(model);
        }

        TempData["SuccessMessage"] = $"Lead successfully converted! Customer #{result.CustomerId} was established." +
            (result.OpportunityId.HasValue ? $" Opportunity #{result.OpportunityId} was also initiated." : "");

        return RedirectToAction("Details", "Customer", new { id = result.CustomerId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, error) = await _leadService.DeleteOrDeactivateAsync(id, CurrentUserId, CurrentUserRole);
        if (!success)
        {
            TempData["ErrorMessage"] = error ?? "Failed to process lead.";
        }
        else
        {
            TempData["SuccessMessage"] = "Lead record processed successfully.";
        }

        return RedirectToAction(nameof(Index));
    }
}
