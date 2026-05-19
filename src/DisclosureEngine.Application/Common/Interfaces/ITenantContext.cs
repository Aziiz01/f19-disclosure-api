namespace DisclosureEngine.Application.Common.Interfaces;

/// <summary>
/// Per-request identity resolved from JWT claims. Null means no auth header was present.
/// </summary>
/// <remarks>
/// EF Core query filters and AuditingInterceptor both read these. Null tenant id → zero rows,
/// not an exception. That's the secure default, not a bug.
/// </remarks>
public interface ITenantContext
{
    /// <summary>Tenant id from the JWT, or null if unauthenticated.</summary>
    Guid? CurrentTenantId { get; }

    /// <summary>User id from the JWT, or null if unauthenticated.</summary>
    Guid? CurrentUserId { get; }
}
