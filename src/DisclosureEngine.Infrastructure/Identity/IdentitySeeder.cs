using DisclosureEngine.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DisclosureEngine.Infrastructure.Identity;

public static class IdentitySeeder
{
    private sealed record SeedUser(string Email, string Password, Guid TenantId, string Role);

    public static async Task SeedAsync(IServiceProvider services, CancellationToken ct = default)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var logger      = services.GetRequiredService<ILoggerFactory>().CreateLogger("IdentitySeeder");

        var seedUsers = new[]
        {
            new SeedUser("admin@acme.test",      "Admin@123!",    SeedData.AcmeTenantId,   "Admin"),
            new SeedUser("reporter@globex.test", "Reporter@123!", SeedData.GlobexTenantId, "Reporter")
        };

        foreach (var s in seedUsers)
        {
            if (await userManager.FindByEmailAsync(s.Email) is not null) continue;

            var user = new ApplicationUser
            {
                Email          = s.Email,
                UserName       = s.Email,
                EmailConfirmed = true,
                TenantId       = s.TenantId,
                Role           = s.Role
            };

            var result = await userManager.CreateAsync(user, s.Password);
            if (!result.Succeeded)
            {
                logger.LogWarning(
                    "Failed to seed user {Email}: {Errors}",
                    s.Email,
                    string.Join("; ", result.Errors.Select(e => $"{e.Code}:{e.Description}")));
            }
        }
    }
}
