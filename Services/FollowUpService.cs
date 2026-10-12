using AcxiomCRM.Data;
using AcxiomCRM.Models;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services;

public class FollowUpService : IFollowUpService
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditService _audit;

    public FollowUpService(ApplicationDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    private IQueryable<FollowUp> ApplyRoleFilter(IQueryable<FollowUp> query, string? userId, string userRole)
    {
        if (userRole == AppRoles.SalesExecutive && !string.IsNullOrEmpty(userId))
        {
            return query.Where(f => f.AssignedToId == userId);
        }
        return query;
    }

    public async Task<List<FollowUp>> GetAllAsync(
        string? userId,
        string userRole,
        FollowUpStatus? status = null,
        FollowUpType? type = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = _db.FollowUps
            .Include(f => f.AssignedTo)
            .Include(f => f.Customer)
            .Include(f => f.Lead)
            .Include(f => f.Opportunity)
            .AsNoTracking()
            .AsQueryable();

        query = ApplyRoleFilter(query, userId, userRole);

        if (status.HasValue)
            query = query.Where(f => f.Status == status.Value);

        if (type.HasValue)
            query = query.Where(f => f.Type == type.Value);

        if (dateFrom.HasValue)
            query = query.Where(f => f.ScheduledDate >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(f => f.ScheduledDate <= dateTo.Value.Date.AddDays(1).AddTicks(-1));

        return await query
            .OrderBy(f => f.ScheduledDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetCountAsync(
        string? userId,
        string userRole,
        FollowUpStatus? status = null,
        FollowUpType? type = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null)
    {
        var query = _db.FollowUps.AsNoTracking().AsQueryable();
        query = ApplyRoleFilter(query, userId, userRole);

        if (status.HasValue)
            query = query.Where(f => f.Status == status.Value);

        if (type.HasValue)
            query = query.Where(f => f.Type == type.Value);

        if (dateFrom.HasValue)
            query = query.Where(f => f.ScheduledDate >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(f => f.ScheduledDate <= dateTo.Value.Date.AddDays(1).AddTicks(-1));

        return await query.CountAsync();
    }

    public async Task<List<FollowUp>> GetUpcomingAsync(string? userId, string userRole, int days = 7)
    {
        var now = DateTime.UtcNow;
        var end = now.AddDays(days);

        var query = _db.FollowUps
            .Include(f => f.Customer)
            .Include(f => f.Lead)
            .Include(f => f.Opportunity)
            .AsNoTracking()
            .Where(f => f.Status == FollowUpStatus.Planned && f.ScheduledDate >= now && f.ScheduledDate <= end);

        query = ApplyRoleFilter(query, userId, userRole);

        return await query.OrderBy(f => f.ScheduledDate).Take(10).ToListAsync();
    }

    public async Task<List<FollowUp>> GetOverdueAsync(string? userId, string userRole)
    {
        var now = DateTime.UtcNow;

        var query = _db.FollowUps
            .Include(f => f.Customer)
            .Include(f => f.Lead)
            .Include(f => f.Opportunity)
            .AsNoTracking()
            .Where(f => f.Status == FollowUpStatus.Planned && f.ScheduledDate < now);

        query = ApplyRoleFilter(query, userId, userRole);

        return await query.OrderBy(f => f.ScheduledDate).Take(10).ToListAsync();
    }

    public async Task<FollowUp?> GetByIdAsync(int id, string? userId, string userRole)
    {
        var query = _db.FollowUps
            .Include(f => f.AssignedTo)
            .Include(f => f.Customer)
            .Include(f => f.Lead)
            .Include(f => f.Opportunity)
            .AsNoTracking()
            .AsQueryable();

        query = ApplyRoleFilter(query, userId, userRole);

        return await query.FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task<(bool Success, string? Error, FollowUp? FollowUp)> CreateAsync(FollowUp followUp, string currentUserId)
    {
        // Business Rule: New/planned follow-up date cannot be earlier than today
        if (followUp.Status == FollowUpStatus.Planned && followUp.ScheduledDate.Date < DateTime.UtcNow.Date)
        {
            return (false, "Scheduled date for a new planned follow-up cannot be in the past.", null);
        }

        followUp.CreatedById = currentUserId;
        followUp.CreatedAtUtc = DateTime.UtcNow;

        _db.FollowUps.Add(followUp);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(
            currentUserId,
            null,
            "Create",
            "FollowUp",
            followUp.Id.ToString(),
            $"Created follow-up '{followUp.Subject}' ({followUp.Type}) scheduled for {followUp.ScheduledDate:yyyy-MM-dd HH:mm}");

        return (true, null, followUp);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(FollowUp followUp, string currentUserId, string userRole)
    {
        var existing = await _db.FollowUps.FirstOrDefaultAsync(f => f.Id == followUp.Id);
        if (existing == null)
            return (false, "Follow-up not found.");

        if (userRole == AppRoles.SalesExecutive && existing.AssignedToId != currentUserId)
            return (false, "You are not authorized to update this follow-up.");

        var oldValues = $"Status={existing.Status}, Date={existing.ScheduledDate:yyyy-MM-dd HH:mm}";

        existing.Subject = followUp.Subject;
        existing.Type = followUp.Type;
        existing.ScheduledDate = followUp.ScheduledDate;
        existing.Status = followUp.Status;
        existing.Notes = followUp.Notes;
        existing.AssignedToId = followUp.AssignedToId;
        existing.CustomerId = followUp.CustomerId;
        existing.LeadId = followUp.LeadId;
        existing.OpportunityId = followUp.OpportunityId;

        await _db.SaveChangesAsync();

        var newValues = $"Status={existing.Status}, Date={existing.ScheduledDate:yyyy-MM-dd HH:mm}";

        await _audit.LogAsync(
            currentUserId,
            null,
            "Update",
            "FollowUp",
            existing.Id.ToString(),
            $"Updated follow-up '{existing.Subject}'",
            oldValues,
            newValues);

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> MarkCompletedAsync(int id, string? notes, string currentUserId, string userRole)
    {
        var existing = await _db.FollowUps.FirstOrDefaultAsync(f => f.Id == id);
        if (existing == null)
            return (false, "Follow-up not found.");

        if (userRole == AppRoles.SalesExecutive && existing.AssignedToId != currentUserId)
            return (false, "You are not authorized to complete this follow-up.");

        existing.Status = FollowUpStatus.Completed;
        existing.CompletedAtUtc = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(notes))
        {
            existing.Notes = string.IsNullOrWhiteSpace(existing.Notes) ? notes : $"{existing.Notes}\nCompletion Note: {notes}";
        }

        await _db.SaveChangesAsync();

        await _audit.LogAsync(
            currentUserId,
            null,
            "Complete",
            "FollowUp",
            existing.Id.ToString(),
            $"Completed follow-up '{existing.Subject}'");

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> RescheduleAsync(int id, DateTime newDate, string? notes, string currentUserId, string userRole)
    {
        if (newDate.Date < DateTime.UtcNow.Date)
            return (false, "Rescheduled date cannot be in the past.");

        var existing = await _db.FollowUps.FirstOrDefaultAsync(f => f.Id == id);
        if (existing == null)
            return (false, "Follow-up not found.");

        if (userRole == AppRoles.SalesExecutive && existing.AssignedToId != currentUserId)
            return (false, "You are not authorized to reschedule this follow-up.");

        var oldDate = existing.ScheduledDate;
        existing.ScheduledDate = newDate;
        existing.Status = FollowUpStatus.Planned;
        if (!string.IsNullOrWhiteSpace(notes))
        {
            existing.Notes = string.IsNullOrWhiteSpace(existing.Notes) ? $"Rescheduled: {notes}" : $"{existing.Notes}\nRescheduled to {newDate:yyyy-MM-dd HH:mm}: {notes}";
        }

        await _db.SaveChangesAsync();

        await _audit.LogAsync(
            currentUserId,
            null,
            "Reschedule",
            "FollowUp",
            existing.Id.ToString(),
            $"Rescheduled follow-up '{existing.Subject}' from {oldDate:yyyy-MM-dd HH:mm} to {newDate:yyyy-MM-dd HH:mm}");

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id, string currentUserId, string userRole)
    {
        var existing = await _db.FollowUps.FirstOrDefaultAsync(f => f.Id == id);
        if (existing == null)
            return (false, "Follow-up not found.");

        if (userRole == AppRoles.SalesExecutive && existing.AssignedToId != currentUserId)
            return (false, "You are not authorized to delete this follow-up.");

        _db.FollowUps.Remove(existing);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(
            currentUserId,
            null,
            "Delete",
            "FollowUp",
            id.ToString(),
            $"Deleted follow-up '{existing.Subject}'");

        return (true, null);
    }
}
