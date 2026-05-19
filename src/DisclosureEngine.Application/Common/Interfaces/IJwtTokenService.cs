namespace DisclosureEngine.Application.Common.Interfaces;

/// <summary>
/// Issues JWTs for authenticated callers. Primitive-typed signature keeps Application free of
/// Identity types — see DECISIONS.md §11 for why.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>Issues a JWT with userId, tenantId, and role claims. HMAC-SHA256, lifetime from config.</summary>
    TokenResult GenerateToken(Guid userId, string email, Guid tenantId, string role);
}

/// <summary>The encoded JWT and its absolute UTC expiry.</summary>
public sealed record TokenResult(string Token, DateTime ExpiresAt);
