namespace AcxiomCRM.Models;

/// <summary>
/// Predefined application roles for AcxiomCRM authorization.
/// </summary>
public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string SalesExecutive = "SalesExecutive";

    public static readonly IReadOnlyList<string> All = [Admin, Manager, SalesExecutive];
}
