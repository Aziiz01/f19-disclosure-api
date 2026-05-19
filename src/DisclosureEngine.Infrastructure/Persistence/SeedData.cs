using DisclosureEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DisclosureEngine.Infrastructure.Persistence;

public static class SeedData
{
    // Fixed tenant IDs so seeding is idempotent across runs and so the user can
    // hit endpoints with these GUIDs as X-Tenant-Id without first looking them up.
    public static readonly Guid AcmeTenantId  = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid GlobexTenantId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static readonly Guid SystemUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        if (await db.Tenants.AnyAsync(ct)) return;

        var acme = new Tenant(AcmeTenantId, "Acme Corp NV");
        var globex = new Tenant(GlobexTenantId, "Globex BV");

        db.Tenants.Add(acme);
        db.Tenants.Add(globex);

        db.Reports.Add(new Report(
            title: "Acme FY2025 Annual Report (Draft)",
            fiscalYear: 2025,
            tenantId: acme.Id,
            createdByUserId: SystemUserId));

        db.Reports.Add(new Report(
            title: "Globex FY2025 Annual Report (Draft)",
            fiscalYear: 2025,
            tenantId: globex.Id,
            createdByUserId: SystemUserId));

        await db.SaveChangesAsync(ct);
    }
}
