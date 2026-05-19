using Microsoft.AspNetCore.Identity;

namespace DisclosureEngine.Infrastructure.Identity;

/// <summary>
/// ASP.NET Identity user (auth surface). Day 2 overlaps with the legacy
/// <c>Domain.Entities.User</c> by design — see <c>docs/DECISIONS.md</c> §11.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public Guid TenantId { get; set; }

    /// <summary>Free-form role label ("Admin"/"Reporter"/"Viewer"). Copied into the JWT <c>role</c> claim.</summary>
    public string Role { get; set; } = string.Empty;
}
