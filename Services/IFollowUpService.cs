using AcxiomCRM.Models;

namespace AcxiomCRM.Services;

public interface IFollowUpService
{
    Task<List<FollowUp>> GetAllAsync(
        string? userId,
        string userRole,
        FollowUpStatus? status = null,
        FollowUpType? type = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        int page = 1,
        int pageSize = 20);

    Task<int> GetCountAsync(
        string? userId,
        string userRole,
        FollowUpStatus? status = null,
        FollowUpType? type = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null);

    Task<List<FollowUp>> GetUpcomingAsync(string? userId, string userRole, int days = 7);
    Task<List<FollowUp>> GetOverdueAsync(string? userId, string userRole);
    Task<FollowUp?> GetByIdAsync(int id, string? userId, string userRole);
    Task<(bool Success, string? Error, FollowUp? FollowUp)> CreateAsync(FollowUp followUp, string currentUserId);
    Task<(bool Success, string? Error)> UpdateAsync(FollowUp followUp, string currentUserId, string userRole);
    Task<(bool Success, string? Error)> MarkCompletedAsync(int id, string? notes, string currentUserId, string userRole);
    Task<(bool Success, string? Error)> RescheduleAsync(int id, DateTime newDate, string? notes, string currentUserId, string userRole);
    Task<(bool Success, string? Error)> DeleteAsync(int id, string currentUserId, string userRole);
}
