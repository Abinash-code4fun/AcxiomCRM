using AcxiomCRM.Models;

namespace AcxiomCRM.Services;

public interface IAuditService
{
    Task LogAsync(
        string? userId,
        string? userName,
        string action,
        string module,
        string? entityId = null,
        string? details = null,
        string? oldValues = null,
        string? newValues = null,
        string? ipAddress = null,
        string result = "Success");

    Task<List<AuditLog>> GetLogsAsync(
        string? userId = null,
        string? module = null,
        string? action = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int maxRecords = 200);
}
