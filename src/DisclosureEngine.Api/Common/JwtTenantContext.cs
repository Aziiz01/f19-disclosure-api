using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DisclosureEngine.Application.Common.Interfaces;

namespace DisclosureEngine.Api.Common;

/// <summary>
/// Reads tenant + user identity from the validated JWT on the current request.
/// Falls back to <c>X-Tenant-Id</c>/<c>X-User-Id</c> headers in <c>Development</c>
/// when no <c>Authorization</c> header is present — see <c>docs/DECISIONS.md</c> §11.
/// </summary>
public sealed class JwtTenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHostEnvironment _env;

    public JwtTenantContext(IHttpContextAccessor httpContextAccessor, IHostEnvironment env)
    {
        _httpContextAccessor = httpContextAccessor;
        _env = env;
    }

    public Guid? CurrentTenantId => ReadGuid("tenant_id", "X-Tenant-Id");
    public Guid? CurrentUserId   => ReadGuid(JwtRegisteredClaimNames.Sub, "X-User-Id", alsoTry: ClaimTypes.NameIdentifier);

    private Guid? ReadGuid(string claimType, string devHeader, string? alsoTry = null)
    {
        var ctx = _httpContextAccessor.HttpContext;
        if (ctx is null) return null;

        var raw = ctx.User.FindFirstValue(claimType);
        if (raw is null && alsoTry is not null) raw = ctx.User.FindFirstValue(alsoTry);
        if (Guid.TryParse(raw, out var fromClaim)) return fromClaim;

        if (_env.IsDevelopment() && !ctx.Request.Headers.ContainsKey("Authorization"))
        {
            var header = ctx.Request.Headers[devHeader].FirstOrDefault();
            if (Guid.TryParse(header, out var fromHeader)) return fromHeader;
        }

        return null;
    }
}
