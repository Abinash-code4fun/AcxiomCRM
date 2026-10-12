using AcxiomCRM.Data;
using AcxiomCRM.Models;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services;

public class LeadService : ILeadService
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditService _audit;
    private readonly ICustomerService _customerService;

    public LeadService(ApplicationDbContext db, IAuditService audit, ICustomerService customerService)
    {
        _db = db;
        _audit = audit;
        _customerService = customerService;
    }

    private IQueryable<Lead> ApplyRoleFilter(IQueryable<Lead> query, string? userId, string userRole)
    {
        if (userRole == AppRoles.SalesExecutive && !string.IsNullOrEmpty(userId))
        {
            return query.Where(l => l.AssignedToId == userId);
        }
        return query;
    }

    public async Task<List<Lead>> GetAllAsync(string? userId, string userRole, string? search = null, LeadStatus? status = null, LeadPriority? priority = null, int page = 1, int pageSize = 20)
    {
        var query = _db.Leads
            .Include(l => l.AssignedTo)
            .Include(l => l.ConvertedCustomer)
            .AsNoTracking()
            .AsQueryable();

        query = ApplyRoleFilter(query, userId, userRole);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(l => l.FirstName.ToLower().Contains(s)
                || l.LastName.ToLower().Contains(s)
                || l.Email.ToLower().Contains(s)
                || (l.Company != null && l.Company.ToLower().Contains(s)));
        }

        if (status.HasValue)
            query = query.Where(l => l.Status == status.Value);

        if (priority.HasValue)
            query = query.Where(l => l.Priority == priority.Value);

        return await query
            .OrderByDescending(l => l.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetCountAsync(string? userId, string userRole, string? search = null, LeadStatus? status = null, LeadPriority? priority = null)
    {
        var query = _db.Leads.AsNoTracking().AsQueryable();
        query = ApplyRoleFilter(query, userId, userRole);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(l => l.FirstName.ToLower().Contains(s)
                || l.LastName.ToLower().Contains(s)
                || l.Email.ToLower().Contains(s)
                || (l.Company != null && l.Company.ToLower().Contains(s)));
        }

        if (status.HasValue)
            query = query.Where(l => l.Status == status.Value);

        if (priority.HasValue)
            query = query.Where(l => l.Priority == priority.Value);

        return await query.CountAsync();
    }

    public async Task<Lead?> GetByIdAsync(int id, string? userId, string userRole)
    {
        var query = _db.Leads
            .Include(l => l.AssignedTo)
            .Include(l => l.ConvertedCustomer)
            .Include(l => l.ConvertedOpportunity)
            .Include(l => l.FollowUps)
                .ThenInclude(f => f.AssignedTo)
            .AsNoTracking()
            .AsQueryable();

        query = ApplyRoleFilter(query, userId, userRole);

        return await query.FirstOrDefaultAsync(l => l.Id == id);
    }

    public async Task<(bool Success, string? Error, Lead? Lead)> CreateAsync(Lead lead, string currentUserId)
    {
        lead.CreatedById = currentUserId;
        lead.CreatedAtUtc = DateTime.UtcNow;

        _db.Leads.Add(lead);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(
            currentUserId,
            null,
            "Create",
            "Lead",
            lead.Id.ToString(),
            $"Created lead '{lead.FullName}' ({lead.Email})");

        return (true, null, lead);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(Lead lead, string currentUserId, string userRole)
    {
        var existing = await _db.Leads.FirstOrDefaultAsync(l => l.Id == lead.Id);
        if (existing == null)
            return (false, "Lead not found.");

        if (userRole == AppRoles.SalesExecutive && existing.AssignedToId != currentUserId)
            return (false, "You are not authorized to update this lead.");

        if (existing.Status == LeadStatus.Converted && lead.Status != LeadStatus.Converted)
            return (false, "A converted lead cannot be moved back to an uncoverted status.");

        var oldValues = $"Status={existing.Status}, Priority={existing.Priority}, AssignedToId={existing.AssignedToId}";

        existing.FirstName = lead.FirstName;
        existing.LastName = lead.LastName;
        existing.Email = lead.Email;
        existing.Phone = lead.Phone;
        existing.Company = lead.Company;
        existing.Source = lead.Source;
        existing.Status = lead.Status;
        existing.Priority = lead.Priority;
        existing.Notes = lead.Notes;
        existing.AssignedToId = lead.AssignedToId;
        existing.ModifiedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        var newValues = $"Status={existing.Status}, Priority={existing.Priority}, AssignedToId={existing.AssignedToId}";

        await _audit.LogAsync(
            currentUserId,
            null,
            "Update",
            "Lead",
            existing.Id.ToString(),
            $"Updated lead '{existing.FullName}'",
            oldValues,
            newValues);

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteOrDeactivateAsync(int id, string currentUserId, string userRole)
    {
        var existing = await _db.Leads.Include(l => l.FollowUps).FirstOrDefaultAsync(l => l.Id == id);
        if (existing == null)
            return (false, "Lead not found.");

        if (userRole == AppRoles.SalesExecutive && existing.AssignedToId != currentUserId)
            return (false, "You are not authorized to modify this lead.");

        if (existing.Status == LeadStatus.Converted)
            return (false, "Converted leads cannot be deleted because of preserved relational history.");

        if (existing.FollowUps.Any())
        {
            existing.Status = LeadStatus.Lost;
            existing.ModifiedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await _audit.LogAsync(
                currentUserId,
                null,
                "Deactivate",
                "Lead",
                id.ToString(),
                $"Marked lead '{existing.FullName}' as Lost (has existing follow-ups)");

            return (true, null);
        }

        _db.Leads.Remove(existing);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(
            currentUserId,
            null,
            "Delete",
            "Lead",
            id.ToString(),
            $"Deleted lead '{existing.FullName}'");

        return (true, null);
    }

    public async Task<LeadConversionResult> ConvertLeadAsync(LeadConversionRequest request, string currentUserId, string userRole)
    {
        var lead = await _db.Leads.FirstOrDefaultAsync(l => l.Id == request.LeadId);
        if (lead == null)
            return new LeadConversionResult(false, "Lead not found.", null, null);

        if (userRole == AppRoles.SalesExecutive && lead.AssignedToId != currentUserId)
            return new LeadConversionResult(false, "You are not authorized to convert this lead.", null, null);

        if (lead.Status == LeadStatus.Converted)
            return new LeadConversionResult(false, "This lead has already been converted.", lead.ConvertedCustomerId, lead.ConvertedOpportunityId);

        // Transactional boundary for conversion
        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            // 1. Check or create Customer
            var existingCustomer = await _db.Customers.FirstOrDefaultAsync(c => c.Email.ToLower() == lead.Email.ToLower());
            Customer targetCustomer;

            if (existingCustomer != null)
            {
                targetCustomer = existingCustomer;
            }
            else
            {
                targetCustomer = new Customer
                {
                    Name = lead.FullName,
                    Email = lead.Email,
                    Phone = lead.Phone,
                    Address = lead.Company != null ? $"Company: {lead.Company}" : null,
                    Status = CustomerStatus.Active,
                    Notes = $"Converted from Lead #{lead.Id} on {DateTime.UtcNow:yyyy-MM-dd}. {lead.Notes}",
                    AssignedToId = lead.AssignedToId ?? currentUserId,
                    CreatedById = currentUserId,
                    CreatedAtUtc = DateTime.UtcNow
                };

                _db.Customers.Add(targetCustomer);
                await _db.SaveChangesAsync();
            }

            // 2. Optionally create Opportunity
            Opportunity? createdOpportunity = null;
            if (request.CreateOpportunity)
            {
                var oppAmount = request.OpportunityAmount ?? 1000m;
                if (oppAmount <= 0)
                {
                    await transaction.RollbackAsync();
                    return new LeadConversionResult(false, "Opportunity amount must be greater than zero.", null, null);
                }

                var closeDate = request.ExpectedCloseDate ?? DateTime.UtcNow.AddDays(30);
                if (closeDate.Date < DateTime.UtcNow.Date)
                {
                    await transaction.RollbackAsync();
                    return new LeadConversionResult(false, "Opportunity expected close date cannot be in the past.", null, null);
                }

                createdOpportunity = new Opportunity
                {
                    Name = !string.IsNullOrWhiteSpace(request.OpportunityName) ? request.OpportunityName : $"Deal: {lead.FullName}",
                    CustomerId = targetCustomer.Id,
                    OwnerId = lead.AssignedToId ?? currentUserId,
                    Stage = OpportunityStage.Qualification,
                    Amount = oppAmount,
                    Probability = 20,
                    ExpectedCloseDate = closeDate,
                    Source = lead.Source.ToString(),
                    Notes = request.Notes,
                    CreatedById = currentUserId,
                    CreatedAtUtc = DateTime.UtcNow
                };

                _db.Opportunities.Add(createdOpportunity);
                await _db.SaveChangesAsync();
            }

            // 3. Update Lead status to Converted and bind FKs
            lead.Status = LeadStatus.Converted;
            lead.ConvertedCustomerId = targetCustomer.Id;
            lead.ConvertedOpportunityId = createdOpportunity?.Id;
            lead.ConvertedAtUtc = DateTime.UtcNow;
            lead.ModifiedAtUtc = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            // Commit all changes atomically
            await transaction.CommitAsync();

            await _audit.LogAsync(
                currentUserId,
                null,
                "Convert",
                "Lead",
                lead.Id.ToString(),
                $"Converted lead '{lead.FullName}' into Customer #{targetCustomer.Id}" + (createdOpportunity != null ? $" and Opportunity #{createdOpportunity.Id}" : ""));

            return new LeadConversionResult(true, null, targetCustomer.Id, createdOpportunity?.Id);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return new LeadConversionResult(false, $"Lead conversion failed: {ex.Message}", null, null);
        }
    }
}
