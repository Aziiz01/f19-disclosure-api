using System.IdentityModel.Tokens.Jwt;
using System.Text;
using DisclosureEngine.Infrastructure.Identity;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DisclosureEngine.UnitTests.Infrastructure;

public sealed class JwtTokenServiceTests
{
    private const string TestKey      = "test-signing-key-that-is-long-enough-for-hs256-purposes-please";
    private const string TestIssuer   = "disclosure-engine-test-issuer";
    private const string TestAudience = "disclosure-engine-test-audience";

    private static JwtTokenService Build(int expirationMinutes = 90)
    {
        var values = new Dictionary<string, string?>
        {
            ["Jwt:Key"]               = TestKey,
            ["Jwt:Issuer"]            = TestIssuer,
            ["Jwt:Audience"]          = TestAudience,
            ["Jwt:ExpirationMinutes"] = expirationMinutes.ToString()
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        return new JwtTokenService(config);
    }

    [Fact]
    public void GenerateToken_ContainsTenantIdClaim()
    {
        var sut = Build();
        var tenantId = Guid.NewGuid();

        var result = sut.GenerateToken(Guid.NewGuid(), "user@test.com", tenantId, "Admin");

        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        token.Claims.Should().Contain(c => c.Type == "tenant_id" && c.Value == tenantId.ToString());
    }

    [Fact]
    public void GenerateToken_HasExpectedExpiration()
    {
        var sut = Build(expirationMinutes: 90);
        var before = DateTime.UtcNow;

        var result = sut.GenerateToken(Guid.NewGuid(), "user@test.com", Guid.NewGuid(), "Admin");

        result.ExpiresAt.Should().BeCloseTo(before.AddMinutes(90), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GenerateToken_IsSignedWithConfiguredKey()
    {
        var sut = Build();

        var result = sut.GenerateToken(Guid.NewGuid(), "user@test.com", Guid.NewGuid(), "Admin");

        var handler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidIssuer              = TestIssuer,
            ValidateAudience         = true,
            ValidAudience            = TestAudience,
            ValidateLifetime         = false, // we only care about signature here
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestKey))
        };

        Action validate = () => handler.ValidateToken(result.Token, validationParameters, out _);
        validate.Should().NotThrow();
    }
}
