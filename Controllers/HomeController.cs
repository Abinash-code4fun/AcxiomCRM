using System.Diagnostics;
using System.Security.Claims;
using AcxiomCRM.Models;
using AcxiomCRM.Models.ViewModels;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly IFollowUpService _followUpService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        IDashboardService dashboardService,
        IFollowUpService followUpService,
        ILogger<HomeController> logger)
    {
        _dashboardService = dashboardService;
        _followUpService = followUpService;
        _logger = logger;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    private string CurrentUserRole => User.IsInRole(AppRoles.Admin) ? AppRoles.Admin :
                                     User.IsInRole(AppRoles.Manager) ? AppRoles.Manager :
                                     AppRoles.SalesExecutive;

    public async Task<IActionResult> Index(string range = "all")
    {
        var kpis = await _dashboardService.GetKpisAsync(CurrentUserId, CurrentUserRole, range);
        var charts = await _dashboardService.GetChartsAsync(CurrentUserId, CurrentUserRole, range);
        var upcoming = await _followUpService.GetUpcomingAsync(CurrentUserId, CurrentUserRole, 7);
        var overdue = await _followUpService.GetOverdueAsync(CurrentUserId, CurrentUserRole);

        var vm = new DashboardViewModel
        {
            Kpis = kpis,
            Charts = charts,
            UpcomingFollowUps = upcoming,
            OverdueFollowUps = overdue,
            DateRange = range,
            UserRole = CurrentUserRole,
            UserName = User.Identity?.Name ?? "User"
        };

        return View(vm);
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
