using AcxiomCRM.Models;
using AcxiomCRM.Services;

namespace AcxiomCRM.Models.ViewModels;

public class DashboardViewModel
{
    public DashboardKpiSummary Kpis { get; set; } = null!;
    public DashboardCharts Charts { get; set; } = null!;
    public List<FollowUp> UpcomingFollowUps { get; set; } = new();
    public List<FollowUp> OverdueFollowUps { get; set; } = new();
    public string DateRange { get; set; } = "all";
    public string UserRole { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
}
