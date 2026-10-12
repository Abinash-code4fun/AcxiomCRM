using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Models;

namespace AcxiomCRM.Data;

/// <summary>
/// Development-only database and identity seeder.
/// Ensures standard application roles exist and initializes a development
/// administrator account sourced safely from configuration or environment variables.
/// </summary>
public static class DbInitializer
{
    public static async Task SeedRolesAndAdminAsync(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger logger)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Gracefully verify database connectivity before attempting identity operations
        try
        {
            if (!await dbContext.Database.CanConnectAsync())
            {
                logger.LogWarning("Database connection cannot be established at this time. Please ensure SQL Server / LocalDB is running and migrations are applied ('dotnet ef database update'). Role and user seeding skipped.");
                return;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not verify database connection. Skipping seeding until the database is available.");
            return;
        }

        // 1. Seed Roles
        foreach (var roleName in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
                if (roleResult.Succeeded)
                {
                    logger.LogInformation("Role '{RoleName}' created successfully.", roleName);
                }
                else
                {
                    logger.LogError("Failed to create role '{RoleName}': {Errors}",
                        roleName,
                        string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                }
            }
        }

        // 2. Seed Initial Development Administrator Account
        // Read credentials from configuration or environment variables, avoiding hardcoded secrets
        var adminEmail = configuration["SeedData:AdminEmail"]
            ?? Environment.GetEnvironmentVariable("SEED_ADMIN_EMAIL")
            ?? "admin@acxiomcrm.local";

        var adminPassword = configuration["SeedData:AdminPassword"]
            ?? Environment.GetEnvironmentVariable("SEED_ADMIN_PASSWORD");

        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            logger.LogInformation("No initial administrator password specified in configuration ('SeedData:AdminPassword') or environment variable ('SEED_ADMIN_PASSWORD'). Administrator account seeding skipped.");
            return;
        }

        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin == null)
        {
            var adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "System Administrator",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            var createResult = await userManager.CreateAsync(adminUser, adminPassword);
            if (createResult.Succeeded)
            {
                var roleResult = await userManager.AddToRoleAsync(adminUser, AppRoles.Admin);
                if (roleResult.Succeeded)
                {
                    logger.LogInformation("Administrator account '{Email}' successfully created and assigned to the '{Role}' role.", adminEmail, AppRoles.Admin);
                }
                else
                {
                    logger.LogError("Failed to assign '{Role}' role to '{Email}': {Errors}",
                        AppRoles.Admin,
                        adminEmail,
                        string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                logger.LogError("Failed to create administrator account '{Email}': {Errors}",
                    adminEmail,
                    string.Join(", ", createResult.Errors.Select(e => e.Description)));
            }
        }
    }
}
