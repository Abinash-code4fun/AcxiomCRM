using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Models;

namespace AcxiomCRM.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<Opportunity> Opportunities => Set<Opportunity>();
    public DbSet<FollowUp> FollowUps => Set<FollowUp>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // ── ApplicationUser ──
        b.Entity<ApplicationUser>(e =>
        {
            e.Property(u => u.FullName).HasMaxLength(150);
        });

        // ── Customer ──
        b.Entity<Customer>(e =>
        {
            e.HasIndex(c => c.Email).IsUnique();
            e.HasOne(c => c.AssignedTo).WithMany().HasForeignKey(c => c.AssignedToId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(c => c.CreatedBy).WithMany().HasForeignKey(c => c.CreatedById).OnDelete(DeleteBehavior.Restrict);
        });

        // ── Lead ──
        b.Entity<Lead>(e =>
        {
            e.HasIndex(l => l.Email);
            e.HasOne(l => l.AssignedTo).WithMany().HasForeignKey(l => l.AssignedToId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(l => l.CreatedBy).WithMany().HasForeignKey(l => l.CreatedById).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(l => l.ConvertedCustomer).WithMany().HasForeignKey(l => l.ConvertedCustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(l => l.ConvertedOpportunity).WithMany().HasForeignKey(l => l.ConvertedOpportunityId).OnDelete(DeleteBehavior.Restrict);
        });

        // ── Opportunity ──
        b.Entity<Opportunity>(e =>
        {
            e.Property(o => o.Amount).HasColumnType("decimal(18,2)");
            e.HasIndex(o => o.Stage);
            e.HasOne(o => o.Customer).WithMany(c => c.Opportunities).HasForeignKey(o => o.CustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(o => o.Owner).WithMany().HasForeignKey(o => o.OwnerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(o => o.CreatedBy).WithMany().HasForeignKey(o => o.CreatedById).OnDelete(DeleteBehavior.Restrict);
        });

        // ── FollowUp ──
        b.Entity<FollowUp>(e =>
        {
            e.HasIndex(f => f.ScheduledDate);
            e.HasIndex(f => f.Status);
            e.HasOne(f => f.AssignedTo).WithMany().HasForeignKey(f => f.AssignedToId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(f => f.CreatedBy).WithMany().HasForeignKey(f => f.CreatedById).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(f => f.Customer).WithMany(c => c.FollowUps).HasForeignKey(f => f.CustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(f => f.Lead).WithMany(l => l.FollowUps).HasForeignKey(f => f.LeadId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(f => f.Opportunity).WithMany(o => o.FollowUps).HasForeignKey(f => f.OpportunityId).OnDelete(DeleteBehavior.Restrict);
        });

        // ── AuditLog ──
        b.Entity<AuditLog>(e =>
        {
            e.HasIndex(a => a.Timestamp);
            e.HasIndex(a => a.UserId);
            e.HasIndex(a => a.Module);
        });
    }
}
