using Microsoft.AspNetCore.Identity;

namespace AcxiomCRM.Models;

/// <summary>
/// Application user extending ASP.NET Core Identity's framework-managed IdentityUser.
/// Never stores passwords or security artifacts directly; all hashing, salting,
/// and tokens are managed securely by the Identity framework.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
