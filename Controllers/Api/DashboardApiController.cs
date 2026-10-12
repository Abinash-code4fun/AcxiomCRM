using System.Security.Claims;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/dashboard")]
[Authorize]
[IgnoreAntiforgeryToken]
public class DashboardApiController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardApiController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    private string CurrentUserRole => User.IsInRole(AppRoles.Admin) ? AppRoles.Admin :
                                     User.IsInRole(AppRoles.Manager) ? AppRoles.Manager :
                                     AppRoles.SalesExecutive;

    [HttpGet("kpis")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetKpis([FromQuery] string? dateRange)
    {
        var kpis = await _dashboardService.GetKpisAsync(CurrentUserId, CurrentUserRole, dateRange);
        return Ok(kpis);
    }

    [HttpGet("charts")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCharts([FromQuery] string? dateRange)
    {
        var charts = await _dashboardService.GetChartsAsync(CurrentUserId, CurrentUserRole, dateRange);
        return Ok(charts);
    }
}
