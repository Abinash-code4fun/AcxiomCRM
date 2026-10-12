using System.Security.Claims;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/followups")]
[Authorize]
[IgnoreAntiforgeryToken]
public class FollowUpsApiController : ControllerBase
{
    private readonly IFollowUpService _followUpService;

    public FollowUpsApiController(IFollowUpService followUpService)
    {
        _followUpService = followUpService;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    private string CurrentUserRole => User.IsInRole(AppRoles.Admin) ? AppRoles.Admin :
                                     User.IsInRole(AppRoles.Manager) ? AppRoles.Manager :
                                     AppRoles.SalesExecutive;

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] FollowUpStatus? status,
        [FromQuery] FollowUpType? type,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var followUps = await _followUpService.GetAllAsync(CurrentUserId, CurrentUserRole, status, type, dateFrom, dateTo, page, pageSize);
        var totalCount = await _followUpService.GetCountAsync(CurrentUserId, CurrentUserRole, status, type, dateFrom, dateTo);

        return Ok(new
        {
            total = totalCount,
            page,
            pageSize,
            data = followUps.Select(f => new
            {
                f.Id,
                f.Subject,
                Type = f.Type.ToString(),
                Status = f.Status.ToString(),
                f.ScheduledDate,
                AssignedTo = f.AssignedTo?.FullName ?? f.AssignedTo?.UserName,
                Customer = f.Customer != null ? new { f.Customer.Id, f.Customer.Name } : null,
                Lead = f.Lead != null ? new { f.Lead.Id, f.Lead.FullName } : null,
                Opportunity = f.Opportunity != null ? new { f.Opportunity.Id, f.Opportunity.Name } : null,
                f.CompletedAtUtc
            })
        });
    }

    [HttpGet("upcoming")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUpcoming([FromQuery] int days = 7)
    {
        var list = await _followUpService.GetUpcomingAsync(CurrentUserId, CurrentUserRole, days);
        return Ok(list.Select(f => new
        {
            f.Id,
            f.Subject,
            Type = f.Type.ToString(),
            f.ScheduledDate,
            Target = f.Customer != null ? f.Customer.Name : (f.Lead != null ? f.Lead.FullName : (f.Opportunity != null ? f.Opportunity.Name : "-"))
        }));
    }

    [HttpGet("overdue")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOverdue()
    {
        var list = await _followUpService.GetOverdueAsync(CurrentUserId, CurrentUserRole);
        return Ok(list.Select(f => new
        {
            f.Id,
            f.Subject,
            Type = f.Type.ToString(),
            f.ScheduledDate,
            Target = f.Customer != null ? f.Customer.Name : (f.Lead != null ? f.Lead.FullName : (f.Opportunity != null ? f.Opportunity.Name : "-"))
        }));
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] FollowUp followUp)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var (success, error, created) = await _followUpService.CreateAsync(followUp, CurrentUserId);
        if (!success)
            return BadRequest(new { error });

        return CreatedAtAction(nameof(GetAll), new { id = created!.Id }, created);
    }

    [HttpPut("{id:int}/complete")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Complete(int id, [FromBody] CompleteRequest request)
    {
        var (success, error) = await _followUpService.MarkCompletedAsync(id, request.Notes, CurrentUserId, CurrentUserRole);
        if (!success)
            return BadRequest(new { error });

        return Ok(new { message = "Follow-up marked as completed." });
    }

    [HttpPut("{id:int}/reschedule")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Reschedule(int id, [FromBody] RescheduleRequest request)
    {
        var (success, error) = await _followUpService.RescheduleAsync(id, request.NewDate, request.Notes, CurrentUserId, CurrentUserRole);
        if (!success)
            return BadRequest(new { error });

        return Ok(new { message = "Follow-up rescheduled successfully." });
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, error) = await _followUpService.DeleteAsync(id, CurrentUserId, CurrentUserRole);
        if (!success)
            return BadRequest(new { error });

        return Ok(new { message = "Follow-up deleted successfully." });
    }

    public record CompleteRequest(string? Notes);
    public record RescheduleRequest(DateTime NewDate, string? Notes);
}
