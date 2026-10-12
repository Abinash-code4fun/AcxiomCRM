using AcxiomCRM.Models;

namespace AcxiomCRM.Services;

public record DashboardKpiSummary(
    int TotalCustomers,
    int TotalLeads,
    int OpenLeads,
    int TotalOpportunities,
    int OpenOpportunities,
    int WonOpportunities,
    int LostOpportunities,
    int PendingFollowUps,
    int OverdueFollowUps,
    decimal TotalPipeline,
    decimal WeightedPipeline
);

public record ChartDataset(
    List<string> Labels,
    List<decimal> Data,
    List<string> BackgroundColors
);

public record DashboardCharts(
    ChartDataset LeadsByStatus,
    ChartDataset OpportunitiesByStage,
    ChartDataset MonthlyPipeline
);

public interface IDashboardService
{
    Task<DashboardKpiSummary> GetKpisAsync(string? userId, string userRole, string? dateRange = null);
    Task<DashboardCharts> GetChartsAsync(string? userId, string userRole, string? dateRange = null);
}
