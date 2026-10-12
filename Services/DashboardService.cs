using AcxiomCRM.Data;
using AcxiomCRM.Models;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _db;

    public DashboardService(ApplicationDbContext db)
    {
        _db = db;
    }

    private (DateTime? Start, DateTime? End) ResolveDateRange(string? range)
    {
        var now = DateTime.UtcNow;
        return range?.ToLower() switch
        {
            "today" => (now.Date, now.Date.AddDays(1).AddTicks(-1)),
            "week" => (now.Date.AddDays(-(int)now.DayOfWeek), now.Date.AddDays(1).AddTicks(-1)),
            "month" => (new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc), now.Date.AddDays(1).AddTicks(-1)),
            _ => (null, null)
        };
    }

    public async Task<DashboardKpiSummary> GetKpisAsync(string? userId, string userRole, string? dateRange = null)
    {
        var (start, end) = ResolveDateRange(dateRange);
        var now = DateTime.UtcNow;

        // 1. Customers
        var custQuery = _db.Customers.AsNoTracking().AsQueryable();
        if (userRole == AppRoles.SalesExecutive && !string.IsNullOrEmpty(userId))
            custQuery = custQuery.Where(c => c.AssignedToId == userId);
        if (start.HasValue) custQuery = custQuery.Where(c => c.CreatedAtUtc >= start.Value && c.CreatedAtUtc <= end!.Value);
        var totalCustomers = await custQuery.CountAsync();

        // 2. Leads
        var leadQuery = _db.Leads.AsNoTracking().AsQueryable();
        if (userRole == AppRoles.SalesExecutive && !string.IsNullOrEmpty(userId))
            leadQuery = leadQuery.Where(l => l.AssignedToId == userId);
        if (start.HasValue) leadQuery = leadQuery.Where(l => l.CreatedAtUtc >= start.Value && l.CreatedAtUtc <= end!.Value);
        var totalLeads = await leadQuery.CountAsync();
        var openLeads = await leadQuery.CountAsync(l => l.Status == LeadStatus.New || l.Status == LeadStatus.Contacted || l.Status == LeadStatus.Qualified);

        // 3. Opportunities
        var oppQuery = _db.Opportunities.AsNoTracking().AsQueryable();
        if (userRole == AppRoles.SalesExecutive && !string.IsNullOrEmpty(userId))
            oppQuery = oppQuery.Where(o => o.OwnerId == userId);
        if (start.HasValue) oppQuery = oppQuery.Where(o => o.CreatedAtUtc >= start.Value && o.CreatedAtUtc <= end!.Value);

        var totalOpportunities = await oppQuery.CountAsync();
        var openOpportunities = await oppQuery.CountAsync(o => o.Stage != OpportunityStage.Won && o.Stage != OpportunityStage.Lost);
        var wonOpportunities = await oppQuery.CountAsync(o => o.Stage == OpportunityStage.Won);
        var lostOpportunities = await oppQuery.CountAsync(o => o.Stage == OpportunityStage.Lost);

        // Pipeline amounts for active opportunities
        var activeOpps = await oppQuery
            .Where(o => o.Stage != OpportunityStage.Won && o.Stage != OpportunityStage.Lost)
            .Select(o => new { o.Amount, o.Probability })
            .ToListAsync();

        decimal totalPipeline = activeOpps.Sum(o => o.Amount);
        decimal weightedPipeline = activeOpps.Sum(o => o.Amount * o.Probability / 100m);

        // 4. Follow-ups
        var followQuery = _db.FollowUps.AsNoTracking().AsQueryable();
        if (userRole == AppRoles.SalesExecutive && !string.IsNullOrEmpty(userId))
            followQuery = followQuery.Where(f => f.AssignedToId == userId);

        var pendingFollowUps = await followQuery.CountAsync(f => f.Status == FollowUpStatus.Planned);
        var overdueFollowUps = await followQuery.CountAsync(f => f.Status == FollowUpStatus.Planned && f.ScheduledDate < now);

        return new DashboardKpiSummary(
            totalCustomers,
            totalLeads,
            openLeads,
            totalOpportunities,
            openOpportunities,
            wonOpportunities,
            lostOpportunities,
            pendingFollowUps,
            overdueFollowUps,
            totalPipeline,
            weightedPipeline
        );
    }

    public async Task<DashboardCharts> GetChartsAsync(string? userId, string userRole, string? dateRange = null)
    {
        var (start, end) = ResolveDateRange(dateRange);

        // 1. Leads by status
        var leadQuery = _db.Leads.AsNoTracking().AsQueryable();
        if (userRole == AppRoles.SalesExecutive && !string.IsNullOrEmpty(userId))
            leadQuery = leadQuery.Where(l => l.AssignedToId == userId);
        if (start.HasValue) leadQuery = leadQuery.Where(l => l.CreatedAtUtc >= start.Value && l.CreatedAtUtc <= end!.Value);

        var leadsGroup = await leadQuery
            .GroupBy(l => l.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var leadLabels = new List<string>();
        var leadData = new List<decimal>();
        var leadColors = new List<string> { "#0d6efd", "#0dcaf0", "#198754", "#6c757d", "#ffc107", "#dc3545" };

        foreach (var status in Enum.GetValues<LeadStatus>())
        {
            leadLabels.Add(status.ToString());
            var match = leadsGroup.FirstOrDefault(g => g.Status == status);
            leadData.Add(match?.Count ?? 0);
        }

        var leadsChart = new ChartDataset(leadLabels, leadData, leadColors);

        // 2. Opportunities by stage
        var oppQuery = _db.Opportunities.AsNoTracking().AsQueryable();
        if (userRole == AppRoles.SalesExecutive && !string.IsNullOrEmpty(userId))
            oppQuery = oppQuery.Where(o => o.OwnerId == userId);
        if (start.HasValue) oppQuery = oppQuery.Where(o => o.CreatedAtUtc >= start.Value && o.CreatedAtUtc <= end!.Value);

        var oppGroup = await oppQuery
            .GroupBy(o => o.Stage)
            .Select(g => new { Stage = g.Key, Total = g.Sum(x => x.Amount) })
            .ToListAsync();

        var stageLabels = new List<string>();
        var stageData = new List<decimal>();
        var stageColors = new List<string> { "#3b82f6", "#8b5cf6", "#f59e0b", "#10b981", "#ef4444" };

        foreach (var stage in Enum.GetValues<OpportunityStage>())
        {
            stageLabels.Add(stage.ToString());
            var match = oppGroup.FirstOrDefault(g => g.Stage == stage);
            stageData.Add(match?.Total ?? 0);
        }

        var stagesChart = new ChartDataset(stageLabels, stageData, stageColors);

        // 3. Monthly Pipeline (last 6 months)
        var monthsLabels = new List<string>();
        var monthsData = new List<decimal>();
        var monthColors = new List<string>();

        var cur = DateTime.UtcNow;
        for (int i = 5; i >= 0; i--)
        {
            var targetMonth = cur.AddMonths(-i);
            var mStart = new DateTime(targetMonth.Year, targetMonth.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var mEnd = mStart.AddMonths(1).AddTicks(-1);

            monthsLabels.Add(mStart.ToString("MMM yyyy"));

            var monthTotal = await oppQuery
                .Where(o => o.CreatedAtUtc >= mStart && o.CreatedAtUtc <= mEnd)
                .SumAsync(o => (decimal?)o.Amount) ?? 0m;

            monthsData.Add(monthTotal);
            monthColors.Add("#4f46e5");
        }

        var monthlyChart = new ChartDataset(monthsLabels, monthsData, monthColors);

        return new DashboardCharts(leadsChart, stagesChart, monthlyChart);
    }
}
