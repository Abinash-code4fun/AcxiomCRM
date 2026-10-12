using System.Security.Claims;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers;

[Authorize]
public class ReportController : Controller
{
    private readonly IReportService _reportService;

    public ReportController(IReportService reportService)
    {
        _reportService = reportService;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    private string CurrentUserRole => User.IsInRole(AppRoles.Admin) ? AppRoles.Admin :
                                     User.IsInRole(AppRoles.Manager) ? AppRoles.Manager :
                                     AppRoles.SalesExecutive;

    public IActionResult Index()
    {
        return View();
    }

    public async Task<IActionResult> ViewReport(string reportType, string? filter = null)
    {
        var result = await FetchReportResultAsync(reportType, filter);
        ViewBag.ReportType = reportType;
        ViewBag.Filter = filter;
        return View(result);
    }

    public async Task<IActionResult> ExportCsv(string reportType, string? filter = null)
    {
        var result = await FetchReportResultAsync(reportType, filter);
        var filename = $"{reportType}_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv";
        return File(result.CsvBytes, "text/csv", filename);
    }

    private async Task<ReportResult> FetchReportResultAsync(string reportType, string? filter)
    {
        return reportType.ToLower() switch
        {
            "customer" => await _reportService.GetCustomerReportAsync(CurrentUserId, CurrentUserRole, filter),
            "lead" => await _reportService.GetLeadReportAsync(CurrentUserId, CurrentUserRole, filter),
            "opportunity" => await _reportService.GetOpportunityReportAsync(CurrentUserId, CurrentUserRole, filter),
            "followup" => await _reportService.GetFollowUpReportAsync(CurrentUserId, CurrentUserRole, filter),
            "conversion" => await _reportService.GetConversionReportAsync(CurrentUserId, CurrentUserRole),
            "useractivity" => await _reportService.GetUserActivityReportAsync(CurrentUserId, CurrentUserRole),
            _ => await _reportService.GetCustomerReportAsync(CurrentUserId, CurrentUserRole, filter)
        };
    }
}
