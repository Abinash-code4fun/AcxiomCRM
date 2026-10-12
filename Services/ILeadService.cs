using AcxiomCRM.Models;

namespace AcxiomCRM.Services;

public record LeadConversionRequest(
    int LeadId,
    bool CreateOpportunity,
    string? OpportunityName = null,
    decimal? OpportunityAmount = null,
    DateTime? ExpectedCloseDate = null,
    string? Notes = null
);

public record LeadConversionResult(
    bool Success,
    string? Error,
    int? CustomerId,
    int? OpportunityId
);

public interface ILeadService
{
    Task<List<Lead>> GetAllAsync(string? userId, string userRole, string? search = null, LeadStatus? status = null, LeadPriority? priority = null, int page = 1, int pageSize = 20);
    Task<int> GetCountAsync(string? userId, string userRole, string? search = null, LeadStatus? status = null, LeadPriority? priority = null);
    Task<Lead?> GetByIdAsync(int id, string? userId, string userRole);
    Task<(bool Success, string? Error, Lead? Lead)> CreateAsync(Lead lead, string currentUserId);
    Task<(bool Success, string? Error)> UpdateAsync(Lead lead, string currentUserId, string userRole);
    Task<(bool Success, string? Error)> DeleteOrDeactivateAsync(int id, string currentUserId, string userRole);
    Task<LeadConversionResult> ConvertLeadAsync(LeadConversionRequest request, string currentUserId, string userRole);
}
