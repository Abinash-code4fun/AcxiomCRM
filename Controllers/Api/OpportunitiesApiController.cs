using System.Security.Claims;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/opportunities")]
[Authorize]
[IgnoreAntiforgeryToken]
public class OpportunitiesApiController : ControllerBase
{
    private readonly IOpportunityService _opportunityService;

    public OpportunitiesApiController(IOpportunityService opportunityService)
    {
        _opportunityService = opportunityService;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    private string CurrentUserRole => User.IsInRole(AppRoles.Admin) ? AppRoles.Admin :
                                     User.IsInRole(AppRoles.Manager) ? AppRoles.Manager :
                                     AppRoles.SalesExecutive;

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] OpportunityStage? stage, [FromQuery] int? customerId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var opps = await _opportunityService.GetAllAsync(CurrentUserId, CurrentUserRole, search, stage, customerId, page, pageSize);
        var totalCount = await _opportunityService.GetCountAsync(CurrentUserId, CurrentUserRole, search, stage, customerId);
        var (totalPipeline, weightedPipeline) = await _opportunityService.GetPipelineSummaryAsync(CurrentUserId, CurrentUserRole);

        return Ok(new
        {
            total = totalCount,
            page,
            pageSize,
            totalPipeline,
            weightedPipeline,
            data = opps.Select(o => new
            {
                o.Id,
                o.Name,
                CustomerId = o.CustomerId,
                CustomerName = o.Customer?.Name,
                OwnerId = o.OwnerId,
                OwnerName = o.Owner?.FullName ?? o.Owner?.UserName,
                Stage = o.Stage.ToString(),
                o.Amount,
                o.Probability,
                o.WeightedValue,
                o.ExpectedCloseDate,
                o.CreatedAtUtc
            })
        });
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var opp = await _opportunityService.GetByIdAsync(id, CurrentUserId, CurrentUserRole);
        if (opp == null)
            return NotFound(new { error = "Opportunity not found or unauthorized access." });

        return Ok(new
        {
            opp.Id,
            opp.Name,
            CustomerId = opp.CustomerId,
            CustomerName = opp.Customer?.Name,
            OwnerId = opp.OwnerId,
            OwnerName = opp.Owner?.FullName ?? opp.Owner?.UserName,
            Stage = opp.Stage.ToString(),
            opp.Amount,
            opp.Probability,
            opp.WeightedValue,
            opp.ExpectedCloseDate,
            opp.Source,
            opp.Notes,
            opp.CreatedAtUtc,
            FollowUps = opp.FollowUps.Select(f => new { f.Id, f.Subject, Type = f.Type.ToString(), Status = f.Status.ToString(), f.ScheduledDate })
        });
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] Opportunity opportunity)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var (success, error, created) = await _opportunityService.CreateAsync(opportunity, CurrentUserId);
        if (!success)
            return BadRequest(new { error });

        return CreatedAtAction(nameof(GetById), new { id = created!.Id }, created);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] Opportunity opportunity)
    {
        if (id != opportunity.Id)
            return BadRequest(new { error = "ID mismatch." });

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var (success, error) = await _opportunityService.UpdateAsync(opportunity, CurrentUserId, CurrentUserRole);
        if (!success)
            return BadRequest(new { error });

        return Ok(new { message = "Opportunity updated successfully." });
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, error) = await _opportunityService.DeleteOrDeactivateAsync(id, CurrentUserId, CurrentUserRole);
        if (!success)
            return BadRequest(new { error });

        return Ok(new { message = "Opportunity processed successfully." });
    }
}
