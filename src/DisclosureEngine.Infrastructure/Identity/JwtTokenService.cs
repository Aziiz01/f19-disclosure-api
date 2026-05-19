using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DisclosureEngine.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DisclosureEngine.Infrastructure.Identity;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public TokenResult GenerateToken(Guid userId, string email, Guid tenantId, string role)
    {
        var section = _configuration.GetSection("Jwt");
        var key      = section["Key"]      ?? throw new InvalidOperationException("Jwt:Key is not configured.");
        var issuer   = section["Issuer"]   ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");
        var audience = section["Audience"] ?? throw new InvalidOperationException("Jwt:Audience is not configured.");
        var minutes  = int.TryParse(section["ExpirationMinutes"], out var m) ? m : 90;

        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var now     = DateTime.UtcNow;
        var expires = now.AddMinutes(minutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub,   userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            new("tenant_id", tenantId.ToString()),
            new("role",      role)
        };

        var token = new JwtSecurityToken(
            issuer:             issuer,
            audience:           audience,
            claims:             claims,
            notBefore:          now,
            expires:            expires,
            signingCredentials: signingCredentials);

        var encoded = new JwtSecurityTokenHandler().WriteToken(token);
        return new TokenResult(encoded, expires);
    }
}
