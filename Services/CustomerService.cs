using AcxiomCRM.Data;
using AcxiomCRM.Models;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services;

public class CustomerService : ICustomerService
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditService _audit;

    public CustomerService(ApplicationDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    private IQueryable<Customer> ApplyRoleFilter(IQueryable<Customer> query, string? userId, string userRole)
    {
        if (userRole == AppRoles.SalesExecutive && !string.IsNullOrEmpty(userId))
        {
            return query.Where(c => c.AssignedToId == userId);
        }
        return query;
    }

    public async Task<List<Customer>> GetAllAsync(string? userId, string userRole, string? search = null, CustomerStatus? status = null, int page = 1, int pageSize = 20)
    {
        var query = _db.Customers
            .Include(c => c.AssignedTo)
            .AsNoTracking()
            .AsQueryable();

        query = ApplyRoleFilter(query, userId, userRole);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(s) || c.Email.ToLower().Contains(s) || (c.Phone != null && c.Phone.Contains(s)));
        }

        if (status.HasValue)
        {
            query = query.Where(c => c.Status == status.Value);
        }

        return await query
            .OrderByDescending(c => c.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetCountAsync(string? userId, string userRole, string? search = null, CustomerStatus? status = null)
    {
        var query = _db.Customers.AsNoTracking().AsQueryable();
        query = ApplyRoleFilter(query, userId, userRole);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(s) || c.Email.ToLower().Contains(s) || (c.Phone != null && c.Phone.Contains(s)));
        }

        if (status.HasValue)
        {
            query = query.Where(c => c.Status == status.Value);
        }

        return await query.CountAsync();
    }

    public async Task<Customer?> GetByIdAsync(int id, string? userId, string userRole)
    {
        var query = _db.Customers
            .Include(c => c.AssignedTo)
            .Include(c => c.Opportunities)
            .Include(c => c.FollowUps)
                .ThenInclude(f => f.AssignedTo)
            .AsNoTracking()
            .AsQueryable();

        query = ApplyRoleFilter(query, userId, userRole);

        return await query.FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<bool> EmailExistsAsync(string email, int? excludeId = null)
    {
        var normEmail = email.Trim().ToLower();
        var query = _db.Customers.AsNoTracking().Where(c => c.Email.ToLower() == normEmail);
        if (excludeId.HasValue)
        {
            query = query.Where(c => c.Id != excludeId.Value);
        }
        return await query.AnyAsync();
    }

    public async Task<(bool Success, string? Error, Customer? Customer)> CreateAsync(Customer customer, string currentUserId)
    {
        if (await EmailExistsAsync(customer.Email))
        {
            return (false, "A customer with this email address already exists.", null);
        }

        customer.CreatedById = currentUserId;
        customer.CreatedAtUtc = DateTime.UtcNow;

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(
            currentUserId,
            null,
            "Create",
            "Customer",
            customer.Id.ToString(),
            $"Created customer '{customer.Name}' ({customer.Email})");

        return (true, null, customer);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(Customer customer, string currentUserId, string userRole)
    {
        var existing = await _db.Customers.FirstOrDefaultAsync(c => c.Id == customer.Id);
        if (existing == null)
        {
            return (false, "Customer not found.");
        }

        if (userRole == AppRoles.SalesExecutive && existing.AssignedToId != currentUserId)
        {
            return (false, "You are not authorized to update this customer record.");
        }

        if (await EmailExistsAsync(customer.Email, customer.Id))
        {
            return (false, "Another customer with this email address already exists.");
        }

        var oldValues = $"Name={existing.Name}, Email={existing.Email}, Status={existing.Status}, AssignedToId={existing.AssignedToId}";

        existing.Name = customer.Name;
        existing.Email = customer.Email;
        existing.Phone = customer.Phone;
        existing.Address = customer.Address;
        existing.Status = customer.Status;
        existing.Notes = customer.Notes;
        existing.AssignedToId = customer.AssignedToId;
        existing.ModifiedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        var newValues = $"Name={existing.Name}, Email={existing.Email}, Status={existing.Status}, AssignedToId={existing.AssignedToId}";

        await _audit.LogAsync(
            currentUserId,
            null,
            "Update",
            "Customer",
            existing.Id.ToString(),
            $"Updated customer '{existing.Name}'",
            oldValues,
            newValues);

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteOrDeactivateAsync(int id, string currentUserId, string userRole)
    {
        var existing = await _db.Customers
            .Include(c => c.Opportunities)
            .Include(c => c.FollowUps)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (existing == null)
            return (false, "Customer not found.");

        if (userRole == AppRoles.SalesExecutive && existing.AssignedToId != currentUserId)
            return (false, "You are not authorized to modify this customer.");

        // If customer has related opportunities or follow-ups, soft-deactivate instead of hard delete to maintain relational integrity
        if (existing.Opportunities.Any() || existing.FollowUps.Any())
        {
            existing.Status = CustomerStatus.Inactive;
            existing.ModifiedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await _audit.LogAsync(
                currentUserId,
                null,
                "Deactivate",
                "Customer",
                id.ToString(),
                $"Deactivated customer '{existing.Name}' (has active dependencies)");

            return (true, null);
        }

        _db.Customers.Remove(existing);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(
            currentUserId,
            null,
            "Delete",
            "Customer",
            id.ToString(),
            $"Deleted customer '{existing.Name}'");

        return (true, null);
    }
}
