using AcxiomCRM.Models;

namespace AcxiomCRM.Services;

public interface ICustomerService
{
    Task<List<Customer>> GetAllAsync(string? userId, string userRole, string? search = null, CustomerStatus? status = null, int page = 1, int pageSize = 20);
    Task<int> GetCountAsync(string? userId, string userRole, string? search = null, CustomerStatus? status = null);
    Task<Customer?> GetByIdAsync(int id, string? userId, string userRole);
    Task<(bool Success, string? Error, Customer? Customer)> CreateAsync(Customer customer, string currentUserId);
    Task<(bool Success, string? Error)> UpdateAsync(Customer customer, string currentUserId, string userRole);
    Task<(bool Success, string? Error)> DeleteOrDeactivateAsync(int id, string currentUserId, string userRole);
    Task<bool> EmailExistsAsync(string email, int? excludeId = null);
}
