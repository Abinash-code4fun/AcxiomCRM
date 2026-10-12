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
public class CustomerController : Controller
{
    private readonly ICustomerService _customerService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<CustomerController> _logger;

    public CustomerController(
        ICustomerService customerService,
        UserManager<ApplicationUser> userManager,
        ILogger<CustomerController> logger)
    {
        _customerService = customerService;
        _userManager = userManager;
        _logger = logger;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    private string CurrentUserRole => User.IsInRole(AppRoles.Admin) ? AppRoles.Admin :
                                     User.IsInRole(AppRoles.Manager) ? AppRoles.Manager :
                                     AppRoles.SalesExecutive;

    private async Task PopulateSalesExecutivesAsync(CustomerFormViewModel model)
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

    public async Task<IActionResult> Index(string? search, CustomerStatus? status, int page = 1)
    {
        const int pageSize = 15;
        var customers = await _customerService.GetAllAsync(CurrentUserId, CurrentUserRole, search, status, page, pageSize);
        var totalCount = await _customerService.GetCountAsync(CurrentUserId, CurrentUserRole, search, status);

        var vm = new CustomerListViewModel
        {
            Customers = customers,
            Search = search,
            Status = status,
            Page = page,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };

        return View(vm);
    }

    public async Task<IActionResult> Details(int id)
    {
        var customer = await _customerService.GetByIdAsync(id, CurrentUserId, CurrentUserRole);
        if (customer == null)
        {
            TempData["ErrorMessage"] = "Customer not found or you are not authorized to view this record.";
            return RedirectToAction(nameof(Index));
        }

        var vm = new CustomerDetailsViewModel
        {
            Customer = customer,
            Opportunities = customer.Opportunities.OrderByDescending(o => o.CreatedAtUtc).ToList(),
            FollowUps = customer.FollowUps.OrderByDescending(f => f.ScheduledDate).ToList()
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var vm = new CustomerFormViewModel
        {
            AssignedToId = CurrentUserRole == AppRoles.SalesExecutive ? CurrentUserId : null
        };
        await PopulateSalesExecutivesAsync(vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CustomerFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateSalesExecutivesAsync(model);
            return View(model);
        }

        var customer = new Customer
        {
            Name = model.Name.Trim(),
            Email = model.Email.Trim().ToLower(),
            Phone = model.Phone?.Trim(),
            Address = model.Address?.Trim(),
            Status = model.Status,
            Notes = model.Notes?.Trim(),
            AssignedToId = model.AssignedToId
        };

        var (success, error, created) = await _customerService.CreateAsync(customer, CurrentUserId);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to create customer.");
            await PopulateSalesExecutivesAsync(model);
            return View(model);
        }

        TempData["SuccessMessage"] = $"Customer '{customer.Name}' was successfully created.";
        return RedirectToAction(nameof(Details), new { id = created!.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var customer = await _customerService.GetByIdAsync(id, CurrentUserId, CurrentUserRole);
        if (customer == null)
        {
            TempData["ErrorMessage"] = "Customer not found or you do not have permission to edit it.";
            return RedirectToAction(nameof(Index));
        }

        var vm = new CustomerFormViewModel
        {
            Id = customer.Id,
            Name = customer.Name,
            Email = customer.Email,
            Phone = customer.Phone,
            Address = customer.Address,
            Status = customer.Status,
            Notes = customer.Notes,
            AssignedToId = customer.AssignedToId
        };

        await PopulateSalesExecutivesAsync(vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CustomerFormViewModel model)
    {
        if (id != model.Id)
            return BadRequest();

        if (!ModelState.IsValid)
        {
            await PopulateSalesExecutivesAsync(model);
            return View(model);
        }

        var customer = new Customer
        {
            Id = model.Id,
            Name = model.Name.Trim(),
            Email = model.Email.Trim().ToLower(),
            Phone = model.Phone?.Trim(),
            Address = model.Address?.Trim(),
            Status = model.Status,
            Notes = model.Notes?.Trim(),
            AssignedToId = model.AssignedToId
        };

        var (success, error) = await _customerService.UpdateAsync(customer, CurrentUserId, CurrentUserRole);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to update customer.");
            await PopulateSalesExecutivesAsync(model);
            return View(model);
        }

        TempData["SuccessMessage"] = $"Customer '{customer.Name}' was successfully updated.";
        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, error) = await _customerService.DeleteOrDeactivateAsync(id, CurrentUserId, CurrentUserRole);
        if (!success)
        {
            TempData["ErrorMessage"] = error ?? "Failed to remove customer.";
        }
        else
        {
            TempData["SuccessMessage"] = "Customer record processed successfully.";
        }

        return RedirectToAction(nameof(Index));
    }
}
