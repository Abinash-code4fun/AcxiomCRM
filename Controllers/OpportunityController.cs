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
public class OpportunityController : Controller
{
    private readonly IOpportunityService _opportunityService;
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<OpportunityController> _logger;

    public OpportunityController(
        IOpportunityService opportunityService,
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        ILogger<OpportunityController> logger)
    {
        _opportunityService = opportunityService;
        _db = db;
        _userManager = userManager;
        _logger = logger;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    private string CurrentUserRole => User.IsInRole(AppRoles.Admin) ? AppRoles.Admin :
                                     User.IsInRole(AppRoles.Manager) ? AppRoles.Manager :
                                     AppRoles.SalesExecutive;

    private async Task PopulateDropdownsAsync(OpportunityFormViewModel model)
    {
        var customers = await _db.Customers
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name
            })
            .ToListAsync();

        var users = await _userManager.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.FullName ?? u.UserName)
            .Select(u => new SelectListItem
            {
                Value = u.Id,
                Text = u.FullName ?? u.UserName
            })
            .ToListAsync();

        model.Customers = customers;
        model.Owners = users;
    }

    public async Task<IActionResult> Index(string? search, OpportunityStage? stage, int? customerId, int page = 1)
    {
        const int pageSize = 15;
        var opportunities = await _opportunityService.GetAllAsync(CurrentUserId, CurrentUserRole, search, stage, customerId, page, pageSize);
        var totalCount = await _opportunityService.GetCountAsync(CurrentUserId, CurrentUserRole, search, stage, customerId);
        var (totalPipeline, weightedPipeline) = await _opportunityService.GetPipelineSummaryAsync(CurrentUserId, CurrentUserRole);

        var vm = new OpportunityListViewModel
        {
            Opportunities = opportunities,
            Search = search,
            Stage = stage,
            Page = page,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            TotalPipeline = totalPipeline,
            WeightedPipeline = weightedPipeline
        };

        return View(vm);
    }

    public async Task<IActionResult> Details(int id)
    {
        var opp = await _opportunityService.GetByIdAsync(id, CurrentUserId, CurrentUserRole);
        if (opp == null)
        {
            TempData["ErrorMessage"] = "Opportunity not found or unauthorized.";
            return RedirectToAction(nameof(Index));
        }

        var vm = new OpportunityDetailsViewModel
        {
            Opportunity = opp,
            FollowUps = opp.FollowUps.OrderByDescending(f => f.ScheduledDate).ToList()
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? customerId)
    {
        var vm = new OpportunityFormViewModel
        {
            CustomerId = customerId ?? 0,
            OwnerId = CurrentUserId,
            ExpectedCloseDate = DateTime.UtcNow.AddDays(30),
            Probability = 20
        };

        await PopulateDropdownsAsync(vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OpportunityFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(model);
            return View(model);
        }

        var opp = new Opportunity
        {
            Name = model.Name.Trim(),
            CustomerId = model.CustomerId,
            OwnerId = model.OwnerId,
            Stage = model.Stage,
            Amount = model.Amount,
            Probability = model.Probability,
            ExpectedCloseDate = model.ExpectedCloseDate,
            Source = model.Source?.Trim(),
            Notes = model.Notes?.Trim()
        };

        var (success, error, created) = await _opportunityService.CreateAsync(opp, CurrentUserId);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to create opportunity.");
            await PopulateDropdownsAsync(model);
            return View(model);
        }

        TempData["SuccessMessage"] = $"Opportunity '{opp.Name}' was successfully created.";
        return RedirectToAction(nameof(Details), new { id = created!.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var opp = await _opportunityService.GetByIdAsync(id, CurrentUserId, CurrentUserRole);
        if (opp == null)
        {
            TempData["ErrorMessage"] = "Opportunity not found or unauthorized.";
            return RedirectToAction(nameof(Index));
        }

        var vm = new OpportunityFormViewModel
        {
            Id = opp.Id,
            Name = opp.Name,
            CustomerId = opp.CustomerId,
            OwnerId = opp.OwnerId,
            Stage = opp.Stage,
            Amount = opp.Amount,
            Probability = opp.Probability,
            ExpectedCloseDate = opp.ExpectedCloseDate,
            Source = opp.Source,
            Notes = opp.Notes
        };

        await PopulateDropdownsAsync(vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, OpportunityFormViewModel model)
    {
        if (id != model.Id)
            return BadRequest();

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(model);
            return View(model);
        }

        var opp = new Opportunity
        {
            Id = model.Id,
            Name = model.Name.Trim(),
            CustomerId = model.CustomerId,
            OwnerId = model.OwnerId,
            Stage = model.Stage,
            Amount = model.Amount,
            Probability = model.Probability,
            ExpectedCloseDate = model.ExpectedCloseDate,
            Source = model.Source?.Trim(),
            Notes = model.Notes?.Trim()
        };

        var (success, error) = await _opportunityService.UpdateAsync(opp, CurrentUserId, CurrentUserRole);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to update opportunity.");
            await PopulateDropdownsAsync(model);
            return View(model);
        }

        TempData["SuccessMessage"] = $"Opportunity '{opp.Name}' was successfully updated.";
        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, error) = await _opportunityService.DeleteOrDeactivateAsync(id, CurrentUserId, CurrentUserRole);
        if (!success)
        {
            TempData["ErrorMessage"] = error ?? "Failed to process opportunity.";
        }
        else
        {
            TempData["SuccessMessage"] = "Opportunity record processed successfully.";
        }

        return RedirectToAction(nameof(Index));
    }
}
