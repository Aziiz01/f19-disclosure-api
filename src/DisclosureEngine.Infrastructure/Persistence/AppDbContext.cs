using DisclosureEngine.Application.Common.Interfaces;
using DisclosureEngine.Domain.Entities;
using DisclosureEngine.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DisclosureEngine.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, IAppDbContext
{
    private readonly ITenantContext _tenantContext;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<XbrlFact> XbrlFacts => Set<XbrlFact>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // Explicit interface implementation: IAppDbContext.Users surfaces the legacy
    // Domain.Entities.User DbSet without colliding with IdentityDbContext.Users
    // (which returns DbSet<ApplicationUser>). See docs/DECISIONS.md §11.
    DbSet<DisclosureEngine.Domain.Entities.User> IAppDbContext.Users => Set<DisclosureEngine.Domain.Entities.User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        modelBuilder.Entity<DisclosureEngine.Domain.Entities.User>()
            .HasQueryFilter(u => u.TenantId == _tenantContext.CurrentTenantId);
        modelBuilder.Entity<Report>()
            .HasQueryFilter(r => r.TenantId == _tenantContext.CurrentTenantId);
        modelBuilder.Entity<XbrlFact>()
            .HasQueryFilter(x => x.TenantId == _tenantContext.CurrentTenantId);
        modelBuilder.Entity<Attachment>()
            .HasQueryFilter(a => a.TenantId == _tenantContext.CurrentTenantId);

        // ApplicationUser column constraints (no tenant filter — login needs to find users
        // without an authenticated tenant context).
        modelBuilder.Entity<ApplicationUser>(b =>
        {
            b.Property(u => u.TenantId).IsRequired();
            b.Property(u => u.Role).HasMaxLength(50).IsRequired();
            b.HasIndex(u => u.TenantId);
        });
    }
}
