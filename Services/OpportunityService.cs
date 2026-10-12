using AcxiomCRM.Data;
using AcxiomCRM.Models;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services;

public class OpportunityService : IOpportunityService
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditService _audit;

    public OpportunityService(ApplicationDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    private IQueryable<Opportunity> ApplyRoleFilter(IQueryable<Opportunity> query, string? userId, string userRole)
    {
        if (userRole == AppRoles.SalesExecutive && !string.IsNullOrEmpty(userId))
        {
            return query.Where(o => o.OwnerId == userId);
        }
        return query;
    }

    public async Task<List<Opportunity>> GetAllAsync(string? userId, string userRole, string? search = null, OpportunityStage? stage = null, int? customerId = null, int page = 1, int pageSize = 20)
    {
        var query = _db.Opportunities
            .Include(o => o.Customer)
            .Include(o => o.Owner)
            .AsNoTracking()
            .AsQueryable();

        query = ApplyRoleFilter(query, userId, userRole);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(o => o.Name.ToLower().Contains(s) || o.Customer!.Name.ToLower().Contains(s));
        }

        if (stage.HasValue)
            query = query.Where(o => o.Stage == stage.Value);

        if (customerId.HasValue)
            query = query.Where(o => o.CustomerId == customerId.Value);

        return await query
            .OrderByDescending(o => o.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetCountAsync(string? userId, string userRole, string? search = null, OpportunityStage? stage = null, int? customerId = null)
    {
        var query = _db.Opportunities.AsNoTracking().AsQueryable();
        query = ApplyRoleFilter(query, userId, userRole);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(o => o.Name.ToLower().Contains(s) || (o.Customer != null && o.Customer.Name.ToLower().Contains(s)));
        }

        if (stage.HasValue)
            query = query.Where(o => o.Stage == stage.Value);

        if (customerId.HasValue)
            query = query.Where(o => o.CustomerId == customerId.Value);

        return await query.CountAsync();
    }

    public async Task<Opportunity?> GetByIdAsync(int id, string? userId, string userRole)
    {
        var query = _db.Opportunities
            .Include(o => o.Customer)
            .Include(o => o.Owner)
            .Include(o => o.FollowUps)
                .ThenInclude(f => f.AssignedTo)
            .AsNoTracking()
            .AsQueryable();

        query = ApplyRoleFilter(query, userId, userRole);

        return await query.FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<(bool Success, string? Error, Opportunity? Opportunity)> CreateAsync(Opportunity opportunity, string currentUserId)
    {
        // Business Rule Validations
        if (opportunity.Amount <= 0)
            return (false, "Amount must be greater than zero.", null);

        if (opportunity.Probability < 0 || opportunity.Probability > 100)
            return (false, "Probability must be between 0 and 100.", null);

        bool isActiveStage = opportunity.Stage != OpportunityStage.Won && opportunity.Stage != OpportunityStage.Lost;
        if (isActiveStage && opportunity.ExpectedCloseDate.Date < DateTime.UtcNow.Date)
            return (false, "Expected close date cannot be in the past for active opportunities.", null);

        var customerExists = await _db.Customers.AnyAsync(c => c.Id == opportunity.CustomerId);
        if (!customerExists)
            return (false, "The selected customer does not exist.", null);

        opportunity.CreatedById = currentUserId;
        opportunity.CreatedAtUtc = DateTime.UtcNow;

        _db.Opportunities.Add(opportunity);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(
            currentUserId,
            null,
            "Create",
            "Opportunity",
            opportunity.Id.ToString(),
            $"Created opportunity '{opportunity.Name}' for Customer #{opportunity.CustomerId} (Amount: {opportunity.Amount:C}, Stage: {opportunity.Stage})");

        return (true, null, opportunity);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(Opportunity opportunity, string currentUserId, string userRole)
    {
        var existing = await _db.Opportunities.FirstOrDefaultAsync(o => o.Id == opportunity.Id);
        if (existing == null)
            return (false, "Opportunity not found.");

        if (userRole == AppRoles.SalesExecutive && existing.OwnerId != currentUserId)
            return (false, "You are not authorized to update this opportunity.");

        if (opportunity.Amount <= 0)
            return (false, "Amount must be greater than zero.");

        if (opportunity.Probability < 0 || opportunity.Probability > 100)
            return (false, "Probability must be between 0 and 100.");

        bool isActiveStage = opportunity.Stage != OpportunityStage.Won && opportunity.Stage != OpportunityStage.Lost;
        if (isActiveStage && opportunity.ExpectedCloseDate.Date < DateTime.UtcNow.Date)
            return (false, "Expected close date cannot be in the past for active opportunities.");

        var oldValues = $"Stage={existing.Stage}, Amount={existing.Amount}, Probability={existing.Probability}, ExpectedCloseDate={existing.ExpectedCloseDate:yyyy-MM-dd}";

        existing.Name = opportunity.Name;
        existing.CustomerId = opportunity.CustomerId;
        existing.OwnerId = opportunity.OwnerId;
        existing.Stage = opportunity.Stage;
        existing.Amount = opportunity.Amount;
        existing.Probability = opportunity.Probability;
        existing.ExpectedCloseDate = opportunity.ExpectedCloseDate;
        existing.Source = opportunity.Source;
        existing.Notes = opportunity.Notes;
        existing.ModifiedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        var newValues = $"Stage={existing.Stage}, Amount={existing.Amount}, Probability={existing.Probability}, ExpectedCloseDate={existing.ExpectedCloseDate:yyyy-MM-dd}";

        await _audit.LogAsync(
            currentUserId,
            null,
            "Update",
            "Opportunity",
            existing.Id.ToString(),
            $"Updated opportunity '{existing.Name}'",
            oldValues,
            newValues);

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteOrDeactivateAsync(int id, string currentUserId, string userRole)
    {
        var existing = await _db.Opportunities.Include(o => o.FollowUps).FirstOrDefaultAsync(o => o.Id == id);
        if (existing == null)
            return (false, "Opportunity not found.");

        if (userRole == AppRoles.SalesExecutive && existing.OwnerId != currentUserId)
            return (false, "You are not authorized to modify this opportunity.");

        if (existing.FollowUps.Any())
        {
            existing.Stage = OpportunityStage.Lost;
            existing.ModifiedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await _audit.LogAsync(
                currentUserId,
                null,
                "Deactivate",
                "Opportunity",
                id.ToString(),
                $"Marked opportunity '{existing.Name}' as Lost (has associated follow-ups)");

            return (true, null);
        }

        _db.Opportunities.Remove(existing);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(
            currentUserId,
            null,
            "Delete",
            "Opportunity",
            id.ToString(),
            $"Deleted opportunity '{existing.Name}'");

        return (true, null);
    }

    public async Task<(decimal TotalPipeline, decimal WeightedPipeline)> GetPipelineSummaryAsync(string? userId, string userRole)
    {
        var query = _db.Opportunities
            .AsNoTracking()
            .Where(o => o.Stage != OpportunityStage.Won && o.Stage != OpportunityStage.Lost);

        query = ApplyRoleFilter(query, userId, userRole);

        var list = await query.Select(o => new { o.Amount, o.Probability }).ToListAsync();

        decimal total = list.Sum(o => o.Amount);
        decimal weighted = list.Sum(o => o.Amount * o.Probability / 100m);

        return (total, weighted);
    }
}
