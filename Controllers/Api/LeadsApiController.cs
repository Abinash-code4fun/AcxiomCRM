using System.Security.Claims;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/leads")]
[Authorize]
[IgnoreAntiforgeryToken]
public class LeadsApiController : ControllerBase
{
    private readonly ILeadService _leadService;

    public LeadsApiController(ILeadService leadService)
    {
        _leadService = leadService;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    private string CurrentUserRole => User.IsInRole(AppRoles.Admin) ? AppRoles.Admin :
                                     User.IsInRole(AppRoles.Manager) ? AppRoles.Manager :
                                     AppRoles.SalesExecutive;

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] LeadStatus? status, [FromQuery] LeadPriority? priority, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var leads = await _leadService.GetAllAsync(CurrentUserId, CurrentUserRole, search, status, priority, page, pageSize);
        var totalCount = await _leadService.GetCountAsync(CurrentUserId, CurrentUserRole, search, status, priority);

        return Ok(new
        {
            total = totalCount,
            page,
            pageSize,
            data = leads.Select(l => new
            {
                l.Id,
                l.FullName,
                l.Email,
                l.Phone,
                l.Company,
                Source = l.Source.ToString(),
                Status = l.Status.ToString(),
                Priority = l.Priority.ToString(),
                AssignedTo = l.AssignedTo?.FullName ?? l.AssignedTo?.UserName,
                l.ConvertedCustomerId,
                l.CreatedAtUtc
            })
        });
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var lead = await _leadService.GetByIdAsync(id, CurrentUserId, CurrentUserRole);
        if (lead == null)
            return NotFound(new { error = "Lead not found or unauthorized access." });

        return Ok(new
        {
            lead.Id,
            lead.FullName,
            lead.FirstName,
            lead.LastName,
            lead.Email,
            lead.Phone,
            lead.Company,
            Source = lead.Source.ToString(),
            Status = lead.Status.ToString(),
            Priority = lead.Priority.ToString(),
            lead.Notes,
            AssignedTo = lead.AssignedTo?.FullName ?? lead.AssignedTo?.UserName,
            lead.ConvertedCustomerId,
            lead.ConvertedOpportunityId,
            lead.ConvertedAtUtc,
            lead.CreatedAtUtc,
            FollowUps = lead.FollowUps.Select(f => new { f.Id, f.Subject, Type = f.Type.ToString(), Status = f.Status.ToString(), f.ScheduledDate })
        });
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] Lead lead)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var (success, error, created) = await _leadService.CreateAsync(lead, CurrentUserId);
        if (!success)
            return BadRequest(new { error });

        return CreatedAtAction(nameof(GetById), new { id = created!.Id }, created);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] Lead lead)
    {
        if (id != lead.Id)
            return BadRequest(new { error = "ID mismatch." });

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var (success, error) = await _leadService.UpdateAsync(lead, CurrentUserId, CurrentUserRole);
        if (!success)
            return BadRequest(new { error });

        return Ok(new { message = "Lead updated successfully." });
    }

    [HttpPost("{id:int}/convert")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Convert(int id, [FromBody] LeadConversionRequest request)
    {
        if (id != request.LeadId)
            return BadRequest(new { error = "LeadId mismatch." });

        var result = await _leadService.ConvertLeadAsync(request, CurrentUserId, CurrentUserRole);
        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok(new
        {
            message = "Lead successfully converted.",
            customerId = result.CustomerId,
            opportunityId = result.OpportunityId
        });
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, error) = await _leadService.DeleteOrDeactivateAsync(id, CurrentUserId, CurrentUserRole);
        if (!success)
            return BadRequest(new { error });

        return Ok(new { message = "Lead processed successfully." });
    }
}
