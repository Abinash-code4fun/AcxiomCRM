using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class AuditLogController : Controller
{
    private readonly IAuditService _auditService;

    public AuditLogController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    public async Task<IActionResult> Index(
        string? userId,
        string? module,
        string? actionName,
        DateTime? startDate,
        DateTime? endDate)
    {
        var logs = await _auditService.GetLogsAsync(userId, module, actionName, startDate, endDate, maxRecords: 300);

        ViewBag.UserId = userId;
        ViewBag.Module = module;
        ViewBag.ActionName = actionName;
        ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
        ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

        return View(logs);
    }
}
