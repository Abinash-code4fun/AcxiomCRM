using System.Security.Claims;
using AcxiomCRM.Data;
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
public class FollowUpController : Controller
{
    private readonly IFollowUpService _followUpService;
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<FollowUpController> _logger;

    public FollowUpController(
        IFollowUpService followUpService,
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        ILogger<FollowUpController> logger)
    {
        _followUpService = followUpService;
        _db = db;
        _userManager = userManager;
        _logger = logger;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    private string CurrentUserRole => User.IsInRole(AppRoles.Admin) ? AppRoles.Admin :
                                     User.IsInRole(AppRoles.Manager) ? AppRoles.Manager :
                                     AppRoles.SalesExecutive;

    private async Task PopulateDropdownsAsync(FollowUpFormViewModel model)
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

        var customers = await _db.Customers
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name
            })
            .ToListAsync();

        var leads = await _db.Leads
            .AsNoTracking()
            .Where(l => l.Status != LeadStatus.Converted)
            .OrderBy(l => l.LastName)
            .Select(l => new SelectListItem
            {
                Value = l.Id.ToString(),
                Text = $"{l.FullName} ({l.Company ?? "Individual"})"
            })
            .ToListAsync();

        var opps = await _db.Opportunities
            .AsNoTracking()
            .Where(o => o.Stage != OpportunityStage.Won && o.Stage != OpportunityStage.Lost)
            .OrderBy(o => o.Name)
            .Select(o => new SelectListItem
            {
                Value = o.Id.ToString(),
                Text = $"{o.Name} (${o.Amount:N0})"
            })
            .ToListAsync();

        model.Users = users;
        model.Customers = customers;
        model.Leads = leads;
        model.Opportunities = opps;
    }

    public async Task<IActionResult> Index(FollowUpStatus? status, FollowUpType? type, int page = 1)
    {
        const int pageSize = 15;
        var followUps = await _followUpService.GetAllAsync(CurrentUserId, CurrentUserRole, status, type, null, null, page, pageSize);
        var totalCount = await _followUpService.GetCountAsync(CurrentUserId, CurrentUserRole, status, type, null, null);
        var upcoming = await _followUpService.GetUpcomingAsync(CurrentUserId, CurrentUserRole, 7);
        var overdue = await _followUpService.GetOverdueAsync(CurrentUserId, CurrentUserRole);

        var vm = new FollowUpListViewModel
        {
            FollowUps = followUps,
            Upcoming = upcoming,
            Overdue = overdue,
            Status = status,
            Type = type,
            Page = page,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? customerId, int? leadId, int? opportunityId)
    {
        var vm = new FollowUpFormViewModel
        {
            AssignedToId = CurrentUserId,
            ScheduledDate = DateTime.UtcNow.AddDays(1).Date.AddHours(10), // Tomorrow at 10 AM UTC
            CustomerId = customerId,
            LeadId = leadId,
            OpportunityId = opportunityId
        };

        await PopulateDropdownsAsync(vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FollowUpFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(model);
            return View(model);
        }

        var followUp = new FollowUp
        {
            Subject = model.Subject.Trim(),
            Type = model.Type,
            ScheduledDate = model.ScheduledDate,
            Status = model.Status,
            Notes = model.Notes?.Trim(),
            AssignedToId = model.AssignedToId,
            CustomerId = model.CustomerId,
            LeadId = model.LeadId,
            OpportunityId = model.OpportunityId
        };

        var (success, error, created) = await _followUpService.CreateAsync(followUp, CurrentUserId);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to create follow-up.");
            await PopulateDropdownsAsync(model);
            return View(model);
        }

        TempData["SuccessMessage"] = $"Follow-up '{followUp.Subject}' scheduled for {followUp.ScheduledDate:yyyy-MM-dd HH:mm}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var followUp = await _followUpService.GetByIdAsync(id, CurrentUserId, CurrentUserRole);
        if (followUp == null)
        {
            TempData["ErrorMessage"] = "Follow-up not found or unauthorized.";
            return RedirectToAction(nameof(Index));
        }

        var vm = new FollowUpFormViewModel
        {
            Id = followUp.Id,
            Subject = followUp.Subject,
            Type = followUp.Type,
            ScheduledDate = followUp.ScheduledDate,
            Status = followUp.Status,
            Notes = followUp.Notes,
            AssignedToId = followUp.AssignedToId,
            CustomerId = followUp.CustomerId,
            LeadId = followUp.LeadId,
            OpportunityId = followUp.OpportunityId
        };

        await PopulateDropdownsAsync(vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, FollowUpFormViewModel model)
    {
        if (id != model.Id)
            return BadRequest();

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(model);
            return View(model);
        }

        var followUp = new FollowUp
        {
            Id = model.Id,
            Subject = model.Subject.Trim(),
            Type = model.Type,
            ScheduledDate = model.ScheduledDate,
            Status = model.Status,
            Notes = model.Notes?.Trim(),
            AssignedToId = model.AssignedToId,
            CustomerId = model.CustomerId,
            LeadId = model.LeadId,
            OpportunityId = model.OpportunityId
        };

        var (success, error) = await _followUpService.UpdateAsync(followUp, CurrentUserId, CurrentUserRole);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to update follow-up.");
            await PopulateDropdownsAsync(model);
            return View(model);
        }

        TempData["SuccessMessage"] = $"Follow-up '{followUp.Subject}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkCompleted(int id, string? notes)
    {
        var (success, error) = await _followUpService.MarkCompletedAsync(id, notes, CurrentUserId, CurrentUserRole);
        if (!success)
        {
            TempData["ErrorMessage"] = error ?? "Failed to complete follow-up.";
        }
        else
        {
            TempData["SuccessMessage"] = "Follow-up marked as completed.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reschedule(int id, DateTime newDate, string? notes)
    {
        var (success, error) = await _followUpService.RescheduleAsync(id, newDate, notes, CurrentUserId, CurrentUserRole);
        if (!success)
        {
            TempData["ErrorMessage"] = error ?? "Failed to reschedule follow-up.";
        }
        else
        {
            TempData["SuccessMessage"] = $"Follow-up successfully rescheduled to {newDate:yyyy-MM-dd HH:mm}.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, error) = await _followUpService.DeleteAsync(id, CurrentUserId, CurrentUserRole);
        if (!success)
        {
            TempData["ErrorMessage"] = error ?? "Failed to delete follow-up.";
        }
        else
        {
            TempData["SuccessMessage"] = "Follow-up deleted successfully.";
        }

        return RedirectToAction(nameof(Index));
    }
}
