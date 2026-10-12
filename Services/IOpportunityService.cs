using AcxiomCRM.Models;

namespace AcxiomCRM.Services;

public interface IOpportunityService
{
    Task<List<Opportunity>> GetAllAsync(string? userId, string userRole, string? search = null, OpportunityStage? stage = null, int? customerId = null, int page = 1, int pageSize = 20);
    Task<int> GetCountAsync(string? userId, string userRole, string? search = null, OpportunityStage? stage = null, int? customerId = null);
    Task<Opportunity?> GetByIdAsync(int id, string? userId, string userRole);
    Task<(bool Success, string? Error, Opportunity? Opportunity)> CreateAsync(Opportunity opportunity, string currentUserId);
    Task<(bool Success, string? Error)> UpdateAsync(Opportunity opportunity, string currentUserId, string userRole);
    Task<(bool Success, string? Error)> DeleteOrDeactivateAsync(int id, string currentUserId, string userRole);
    Task<(decimal TotalPipeline, decimal WeightedPipeline)> GetPipelineSummaryAsync(string? userId, string userRole);
}
