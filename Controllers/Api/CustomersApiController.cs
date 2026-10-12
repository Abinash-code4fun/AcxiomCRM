using System.Security.Claims;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/customers")]
[Authorize]
[IgnoreAntiforgeryToken]
public class CustomersApiController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersApiController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    private string CurrentUserRole => User.IsInRole(AppRoles.Admin) ? AppRoles.Admin :
                                     User.IsInRole(AppRoles.Manager) ? AppRoles.Manager :
                                     AppRoles.SalesExecutive;

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] CustomerStatus? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var customers = await _customerService.GetAllAsync(CurrentUserId, CurrentUserRole, search, status, page, pageSize);
        var totalCount = await _customerService.GetCountAsync(CurrentUserId, CurrentUserRole, search, status);

        return Ok(new
        {
            total = totalCount,
            page,
            pageSize,
            data = customers.Select(c => new
            {
                c.Id,
                c.Name,
                c.Email,
                c.Phone,
                c.Address,
                Status = c.Status.ToString(),
                AssignedTo = c.AssignedTo?.FullName ?? c.AssignedTo?.UserName,
                c.CreatedAtUtc
            })
        });
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var customer = await _customerService.GetByIdAsync(id, CurrentUserId, CurrentUserRole);
        if (customer == null)
            return NotFound(new { error = "Customer not found or unauthorized access." });

        return Ok(new
        {
            customer.Id,
            customer.Name,
            customer.Email,
            customer.Phone,
            customer.Address,
            Status = customer.Status.ToString(),
            customer.Notes,
            AssignedTo = customer.AssignedTo?.FullName ?? customer.AssignedTo?.UserName,
            customer.CreatedAtUtc,
            Opportunities = customer.Opportunities.Select(o => new { o.Id, o.Name, o.Amount, Stage = o.Stage.ToString() }),
            FollowUps = customer.FollowUps.Select(f => new { f.Id, f.Subject, Type = f.Type.ToString(), Status = f.Status.ToString(), f.ScheduledDate })
        });
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] Customer customer)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var (success, error, created) = await _customerService.CreateAsync(customer, CurrentUserId);
        if (!success)
        {
            if (error != null && error.Contains("already exists", StringComparison.OrdinalIgnoreCase))
                return Conflict(new { error });

            return BadRequest(new { error });
        }

        return CreatedAtAction(nameof(GetById), new { id = created!.Id }, created);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] Customer customer)
    {
        if (id != customer.Id)
            return BadRequest(new { error = "ID mismatch." });

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var (success, error) = await _customerService.UpdateAsync(customer, CurrentUserId, CurrentUserRole);
        if (!success)
            return BadRequest(new { error });

        return Ok(new { message = "Customer updated successfully." });
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, error) = await _customerService.DeleteOrDeactivateAsync(id, CurrentUserId, CurrentUserRole);
        if (!success)
            return BadRequest(new { error });

        return Ok(new { message = "Customer deleted or deactivated successfully." });
    }
}
