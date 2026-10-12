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

[Authorize(Roles = AppRoles.Admin)]
public class UserManagementController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IAuditService _auditService;
    private readonly ILogger<UserManagementController> _logger;

    public UserManagementController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IAuditService auditService,
        ILogger<UserManagementController> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _auditService = auditService;
        _logger = logger;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    private async Task PopulateRolesAsync(IEnumerable<SelectListItem>? existingList, Action<IEnumerable<SelectListItem>> assigner)
    {
        var roles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync();
        assigner(roles.Select(r => new SelectListItem { Value = r, Text = r }));
    }

    public async Task<IActionResult> Index(string? search, string? roleFilter)
    {
        var query = _userManager.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(u => (u.FullName != null && u.FullName.ToLower().Contains(s)) ||
                                     (u.Email != null && u.Email.ToLower().Contains(s)));
        }

        var users = await query.OrderBy(u => u.FullName ?? u.UserName).ToListAsync();
        var vmList = new List<UserListItemViewModel>();

        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            var primaryRole = roles.FirstOrDefault() ?? "None";

            if (!string.IsNullOrWhiteSpace(roleFilter) && primaryRole != roleFilter)
                continue;

            vmList.Add(new UserListItemViewModel
            {
                Id = u.Id,
                FullName = u.FullName ?? u.UserName ?? "Unknown",
                Email = u.Email ?? string.Empty,
                Role = primaryRole,
                IsActive = u.IsActive,
                IsLockedOut = u.LockoutEnd.HasValue && u.LockoutEnd > DateTimeOffset.UtcNow,
                CreatedAtUtc = u.CreatedAtUtc
            });
        }

        var vm = new UserListViewModel
        {
            Users = vmList,
            Search = search,
            RoleFilter = roleFilter
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var vm = new UserCreateViewModel();
        await PopulateRolesAsync(null, roles => vm.Roles = roles);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateRolesAsync(null, roles => model.Roles = roles);
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email.Trim().ToLower(),
            Email = model.Email.Trim().ToLower(),
            FullName = model.FullName.Trim(),
            IsActive = model.IsActive,
            CreatedAtUtc = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var err in result.Errors)
            {
                ModelState.AddModelError(string.Empty, err.Description);
            }
            await PopulateRolesAsync(null, roles => model.Roles = roles);
            return View(model);
        }

        if (!string.IsNullOrEmpty(model.Role))
        {
            await _userManager.AddToRoleAsync(user, model.Role);
        }

        await _auditService.LogAsync(
            CurrentUserId,
            User.Identity?.Name,
            "Create",
            "UserManagement",
            user.Id,
            $"Created user account '{user.Email}' with role '{model.Role}'");

        TempData["SuccessMessage"] = $"User '{user.FullName}' ({user.Email}) was created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            TempData["ErrorMessage"] = "User not found.";
            return RedirectToAction(nameof(Index));
        }

        var userRoles = await _userManager.GetRolesAsync(user);
        var vm = new UserEditViewModel
        {
            Id = user.Id,
            FullName = user.FullName ?? user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            Role = userRoles.FirstOrDefault() ?? AppRoles.SalesExecutive,
            IsActive = user.IsActive
        };

        await PopulateRolesAsync(null, roles => vm.Roles = roles);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, UserEditViewModel model)
    {
        if (id != model.Id)
            return BadRequest();

        if (!ModelState.IsValid)
        {
            await PopulateRolesAsync(null, roles => model.Roles = roles);
            return View(model);
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            TempData["ErrorMessage"] = "User not found.";
            return RedirectToAction(nameof(Index));
        }

        // Critical Safeguard: Prevent deactivating or demoting the last active administrator
        var currentRoles = await _userManager.GetRolesAsync(user);
        bool wasAdmin = currentRoles.Contains(AppRoles.Admin);
        bool remainsAdmin = model.Role == AppRoles.Admin;

        if (wasAdmin && (!model.IsActive || !remainsAdmin))
        {
            var adminUsers = await _userManager.GetUsersInRoleAsync(AppRoles.Admin);
            var activeAdminCount = adminUsers.Count(u => u.IsActive && u.Id != user.Id);
            if (activeAdminCount == 0)
            {
                ModelState.AddModelError(string.Empty, "Cannot deactivate or remove the Admin role from this user because they are the sole remaining active Administrator in the system.");
                await PopulateRolesAsync(null, roles => model.Roles = roles);
                return View(model);
            }
        }

        user.FullName = model.FullName.Trim();
        user.IsActive = model.IsActive;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            foreach (var err in updateResult.Errors)
                ModelState.AddModelError(string.Empty, err.Description);

            await PopulateRolesAsync(null, roles => model.Roles = roles);
            return View(model);
        }

        // Update role
        if (!currentRoles.Contains(model.Role))
        {
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, model.Role);
        }

        await _auditService.LogAsync(
            CurrentUserId,
            User.Identity?.Name,
            "Update",
            "UserManagement",
            user.Id,
            $"Updated user '{user.Email}' (Active: {user.IsActive}, Role: {model.Role})");

        TempData["SuccessMessage"] = $"User '{user.FullName}' was updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ResetPassword(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            TempData["ErrorMessage"] = "User not found.";
            return RedirectToAction(nameof(Index));
        }

        var vm = new UserResetPasswordViewModel
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName ?? user.UserName ?? string.Empty
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(UserResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByIdAsync(model.Id);
        if (user == null)
        {
            TempData["ErrorMessage"] = "User not found.";
            return RedirectToAction(nameof(Index));
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var err in result.Errors)
                ModelState.AddModelError(string.Empty, err.Description);

            return View(model);
        }

        await _auditService.LogAsync(
            CurrentUserId,
            User.Identity?.Name,
            "ResetPassword",
            "UserManagement",
            user.Id,
            $"Administrator reset password for user '{user.Email}'");

        TempData["SuccessMessage"] = $"Password for '{user.Email}' has been reset successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlock(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound();

        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.ResetAccessFailedCountAsync(user);

        await _auditService.LogAsync(
            CurrentUserId,
            User.Identity?.Name,
            "UnlockAccount",
            "UserManagement",
            user.Id,
            $"Unlocked account for user '{user.Email}'");

        TempData["SuccessMessage"] = $"Account '{user.Email}' has been unlocked.";
        return RedirectToAction(nameof(Index));
    }
}
