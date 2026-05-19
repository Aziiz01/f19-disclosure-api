using DisclosureEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DisclosureEngine.Application.Common.Interfaces;

/// <summary>
/// Persistence boundary for the Application layer. AppDbContext applies tenant query filters
/// automatically — LINQ here is already scoped to the current tenant.
/// </summary>
public interface IAppDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<User> Users { get; }
    DbSet<Report> Reports { get; }
    DbSet<XbrlFact> XbrlFacts { get; }
    DbSet<Attachment> Attachments { get; }
    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
