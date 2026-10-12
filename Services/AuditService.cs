using AcxiomCRM.Data;
using AcxiomCRM.Models;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<AuditService> _logger;

    public AuditService(ApplicationDbContext db, ILogger<AuditService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task LogAsync(
        string? userId,
        string? userName,
        string action,
        string module,
        string? entityId = null,
        string? details = null,
        string? oldValues = null,
        string? newValues = null,
        string? ipAddress = null,
        string result = "Success")
    {
        try
        {
            var log = new AuditLog
            {
                UserId = userId,
                UserName = userName,
                Action = action,
                Module = module,
                EntityId = entityId,
                Details = details,
                OldValues = oldValues,
                NewValues = newValues,
                IpAddress = ipAddress,
                Timestamp = DateTime.UtcNow,
                Result = result
            };

            _db.AuditLogs.Add(log);
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Logging failure must not crash the caller or expose sensitive database errors, but is logged on server
            _logger.LogError(ex, "Failed to persist audit log entry for Action: {Action}, Module: {Module}", action, module);
        }
    }

    public async Task<List<AuditLog>> GetLogsAsync(
        string? userId = null,
        string? module = null,
        string? action = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int maxRecords = 200)
    {
        var query = _db.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(userId))
            query = query.Where(a => a.UserId == userId);

        if (!string.IsNullOrWhiteSpace(module))
            query = query.Where(a => a.Module == module);

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(a => a.Action == action);

        if (startDate.HasValue)
            query = query.Where(a => a.Timestamp >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(a => a.Timestamp <= endDate.Value.Date.AddDays(1).AddTicks(-1));

        return await query
            .OrderByDescending(a => a.Timestamp)
            .Take(maxRecords)
            .ToListAsync();
    }
}
