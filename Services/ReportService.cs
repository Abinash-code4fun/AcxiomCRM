using System.Text;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services;

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _db;

    public ReportService(ApplicationDbContext db)
    {
        _db = db;
    }

    private byte[] GenerateCsv(List<string> headers, List<List<string>> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", headers.Select(h => $"\"{EscapeCsv(h)}\"")));

        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(",", row.Select(c => $"\"{EscapeCsv(c)}\"")));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private string EscapeCsv(string val)
    {
        if (string.IsNullOrEmpty(val)) return string.Empty;
        return val.Replace("\"", "\"\"");
    }

    public async Task<ReportResult> GetCustomerReportAsync(string? userId, string userRole, string? status = null)
    {
        var query = _db.Customers
            .Include(c => c.AssignedTo)
            .Include(c => c.Opportunities)
            .AsNoTracking()
            .AsQueryable();

        if (userRole == AppRoles.SalesExecutive && !string.IsNullOrEmpty(userId))
            query = query.Where(c => c.AssignedToId == userId);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<CustomerStatus>(status, true, out var parsedStatus))
            query = query.Where(c => c.Status == parsedStatus);

        var list = await query.OrderByDescending(c => c.CreatedAtUtc).ToListAsync();

        var headers = new List<string> { "Customer ID", "Name", "Email", "Phone", "Status", "Assigned To", "Total Opportunities", "Created Date" };
        var rows = list.Select(c => new List<string>
        {
            c.Id.ToString(),
            c.Name,
            c.Email,
            c.Phone ?? "-",
            c.Status.ToString(),
            c.AssignedTo?.FullName ?? c.AssignedTo?.UserName ?? "Unassigned",
            c.Opportunities.Count.ToString(),
            c.CreatedAtUtc.ToString("yyyy-MM-dd HH:mm")
        }).ToList();

        return new ReportResult("Customer Report", headers, rows, GenerateCsv(headers, rows));
    }

    public async Task<ReportResult> GetLeadReportAsync(string? userId, string userRole, string? status = null)
    {
        var query = _db.Leads
            .Include(l => l.AssignedTo)
            .AsNoTracking()
            .AsQueryable();

        if (userRole == AppRoles.SalesExecutive && !string.IsNullOrEmpty(userId))
            query = query.Where(l => l.AssignedToId == userId);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<LeadStatus>(status, true, out var parsedStatus))
            query = query.Where(l => l.Status == parsedStatus);

        var list = await query.OrderByDescending(l => l.CreatedAtUtc).ToListAsync();

        var headers = new List<string> { "Lead ID", "Name", "Email", "Company", "Source", "Status", "Priority", "Assigned To", "Created Date" };
        var rows = list.Select(l => new List<string>
        {
            l.Id.ToString(),
            l.FullName,
            l.Email,
            l.Company ?? "-",
            l.Source.ToString(),
            l.Status.ToString(),
            l.Priority.ToString(),
            l.AssignedTo?.FullName ?? l.AssignedTo?.UserName ?? "Unassigned",
            l.CreatedAtUtc.ToString("yyyy-MM-dd HH:mm")
        }).ToList();

        return new ReportResult("Lead Report", headers, rows, GenerateCsv(headers, rows));
    }

    public async Task<ReportResult> GetOpportunityReportAsync(string? userId, string userRole, string? stage = null)
    {
        var query = _db.Opportunities
            .Include(o => o.Customer)
            .Include(o => o.Owner)
            .AsNoTracking()
            .AsQueryable();

        if (userRole == AppRoles.SalesExecutive && !string.IsNullOrEmpty(userId))
            query = query.Where(o => o.OwnerId == userId);

        if (!string.IsNullOrWhiteSpace(stage) && Enum.TryParse<OpportunityStage>(stage, true, out var parsedStage))
            query = query.Where(o => o.Stage == parsedStage);

        var list = await query.OrderByDescending(o => o.CreatedAtUtc).ToListAsync();

        var headers = new List<string> { "Opportunity ID", "Deal Name", "Customer", "Owner", "Stage", "Amount ($)", "Probability (%)", "Weighted Value ($)", "Close Date" };
        var rows = list.Select(o => new List<string>
        {
            o.Id.ToString(),
            o.Name,
            o.Customer?.Name ?? "-",
            o.Owner?.FullName ?? o.Owner?.UserName ?? "-",
            o.Stage.ToString(),
            o.Amount.ToString("F2"),
            o.Probability.ToString(),
            o.WeightedValue.ToString("F2"),
            o.ExpectedCloseDate.ToString("yyyy-MM-dd")
        }).ToList();

        return new ReportResult("Opportunity Pipeline Report", headers, rows, GenerateCsv(headers, rows));
    }

    public async Task<ReportResult> GetFollowUpReportAsync(string? userId, string userRole, string? status = null)
    {
        var query = _db.FollowUps
            .Include(f => f.AssignedTo)
            .Include(f => f.Customer)
            .Include(f => f.Lead)
            .Include(f => f.Opportunity)
            .AsNoTracking()
            .AsQueryable();

        if (userRole == AppRoles.SalesExecutive && !string.IsNullOrEmpty(userId))
            query = query.Where(f => f.AssignedToId == userId);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<FollowUpStatus>(status, true, out var parsedStatus))
            query = query.Where(f => f.Status == parsedStatus);

        var list = await query.OrderByDescending(f => f.ScheduledDate).ToListAsync();

        var headers = new List<string> { "ID", "Subject", "Type", "Status", "Scheduled Date", "Assigned To", "Related To", "Completed Date" };
        var rows = list.Select(f => new List<string>
        {
            f.Id.ToString(),
            f.Subject,
            f.Type.ToString(),
            f.Status.ToString(),
            f.ScheduledDate.ToString("yyyy-MM-dd HH:mm"),
            f.AssignedTo?.FullName ?? f.AssignedTo?.UserName ?? "-",
            f.Customer != null ? $"Customer: {f.Customer.Name}" : (f.Lead != null ? $"Lead: {f.Lead.FullName}" : (f.Opportunity != null ? $"Deal: {f.Opportunity.Name}" : "-")),
            f.CompletedAtUtc.HasValue ? f.CompletedAtUtc.Value.ToString("yyyy-MM-dd HH:mm") : "-"
        }).ToList();

        return new ReportResult("Follow-Up Activity Report", headers, rows, GenerateCsv(headers, rows));
    }

    public async Task<ReportResult> GetConversionReportAsync(string? userId, string userRole)
    {
        var query = _db.Leads
            .Include(l => l.AssignedTo)
            .Include(l => l.ConvertedCustomer)
            .Include(l => l.ConvertedOpportunity)
            .AsNoTracking()
            .Where(l => l.Status == LeadStatus.Converted)
            .AsQueryable();

        if (userRole == AppRoles.SalesExecutive && !string.IsNullOrEmpty(userId))
            query = query.Where(l => l.AssignedToId == userId);

        var list = await query.OrderByDescending(l => l.ConvertedAtUtc).ToListAsync();

        var headers = new List<string> { "Lead ID", "Lead Name", "Converted Customer", "Created Opportunity", "Owner", "Converted Date" };
        var rows = list.Select(l => new List<string>
        {
            l.Id.ToString(),
            l.FullName,
            l.ConvertedCustomer?.Name ?? $"Customer #{l.ConvertedCustomerId}",
            l.ConvertedOpportunity != null ? l.ConvertedOpportunity.Name : "None",
            l.AssignedTo?.FullName ?? l.AssignedTo?.UserName ?? "-",
            l.ConvertedAtUtc.HasValue ? l.ConvertedAtUtc.Value.ToString("yyyy-MM-dd HH:mm") : "-"
        }).ToList();

        return new ReportResult("Lead Conversion Report", headers, rows, GenerateCsv(headers, rows));
    }

    public async Task<ReportResult> GetUserActivityReportAsync(string? userId, string userRole)
    {
        var query = _db.AuditLogs
            .AsNoTracking()
            .AsQueryable();

        if (userRole == AppRoles.SalesExecutive && !string.IsNullOrEmpty(userId))
            query = query.Where(a => a.UserId == userId);

        var list = await query.OrderByDescending(a => a.Timestamp).Take(200).ToListAsync();

        var headers = new List<string> { "ID", "Timestamp (UTC)", "User", "Action", "Module", "Entity ID", "Details", "Result" };
        var rows = list.Select(a => new List<string>
        {
            a.Id.ToString(),
            a.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
            a.UserName ?? a.UserId ?? "System",
            a.Action,
            a.Module,
            a.EntityId ?? "-",
            a.Details ?? "-",
            a.Result
        }).ToList();

        return new ReportResult("User Activity & Audit Report", headers, rows, GenerateCsv(headers, rows));
    }
}
