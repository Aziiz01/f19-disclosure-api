using System.Text.Json;
using DisclosureEngine.Application.Common.Interfaces;
using DisclosureEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DisclosureEngine.Infrastructure.Interceptors;

public sealed class AuditingInterceptor : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };

    private readonly ITenantContext _tenantContext;

    public AuditingInterceptor(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        AppendAuditEntries(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        AppendAuditEntries(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    private void AppendAuditEntries(DbContext? context)
    {
        if (context is null) return;

        var auditable = context.ChangeTracker.Entries()
            .Where(e => e.Entity is not AuditLog
                && (e.State == EntityState.Added || e.State == EntityState.Modified))
            .ToList();

        foreach (var entry in auditable)
        {
            var entityTypeName = entry.Entity.GetType().Name;
            var action = $"{entityTypeName}.{entry.State}";
            var entityId = ExtractEntityId(entry);
            var details = SerializeChangedProperties(entry);

            var audit = new AuditLog(
                tenantId: _tenantContext.CurrentTenantId,
                userId: _tenantContext.CurrentUserId,
                action: action,
                entityType: entityTypeName,
                entityId: entityId,
                details: details);

            context.Add(audit);
        }
    }

    private static Guid ExtractEntityId(EntityEntry entry)
    {
        var idProperty = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "Id");
        return idProperty?.CurrentValue is Guid g ? g : Guid.Empty;
    }

    private static string SerializeChangedProperties(EntityEntry entry)
    {
        var snapshot = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var prop in entry.Properties)
        {
            var include = entry.State == EntityState.Added || prop.IsModified;
            if (!include) continue;

            // Never write password hashes or other secret-flagged properties into audit details.
            if (string.Equals(prop.Metadata.Name, "PasswordHash", StringComparison.Ordinal))
            {
                snapshot[prop.Metadata.Name] = "***redacted***";
                continue;
            }

            snapshot[prop.Metadata.Name] = prop.CurrentValue;
        }

        try
        {
            return JsonSerializer.Serialize(snapshot, JsonOptions);
        }
        catch
        {
            return "{}";
        }
    }
}
