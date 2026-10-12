namespace AcxiomCRM.Services;

public record ReportRow(Dictionary<string, string> Data);

public record ReportResult(
    string Title,
    List<string> Headers,
    List<List<string>> Rows,
    byte[] CsvBytes
);

public interface IReportService
{
    Task<ReportResult> GetCustomerReportAsync(string? userId, string userRole, string? status = null);
    Task<ReportResult> GetLeadReportAsync(string? userId, string userRole, string? status = null);
    Task<ReportResult> GetOpportunityReportAsync(string? userId, string userRole, string? stage = null);
    Task<ReportResult> GetFollowUpReportAsync(string? userId, string userRole, string? status = null);
    Task<ReportResult> GetConversionReportAsync(string? userId, string userRole);
    Task<ReportResult> GetUserActivityReportAsync(string? userId, string userRole);
}
