using DisclosureEngine.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace DisclosureEngine.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by <c>dotnet ef migrations</c>.
/// Reads <c>ConnectionStrings:DefaultMigrations</c> so EF tooling targets the
/// Supabase session pooler (port 5432) instead of the transaction pooler the app uses at runtime.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private const string ApiUserSecretsId = "disclosure-engine-secrets";

    public AppDbContext CreateDbContext(string[] args)
    {
        var apiProjectPath = LocateApiProject();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(apiProjectPath)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
            .AddUserSecrets(ApiUserSecretsId)
            .AddEnvironmentVariables()
            .Build();

        var connectionString =
            configuration.GetConnectionString("DefaultMigrations")
            ?? configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Neither ConnectionStrings:DefaultMigrations nor ConnectionStrings:Default is configured. " +
                "Set via 'dotnet user-secrets set' on the API project.");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AppDbContext(options, new DesignTimeTenantContext());
    }

    private static string LocateApiProject()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "DisclosureEngine.sln")))
        {
            dir = dir.Parent;
        }

        if (dir is null)
            throw new InvalidOperationException("Unable to locate DisclosureEngine.sln above the build output directory.");

        var apiPath = Path.Combine(dir.FullName, "src", "DisclosureEngine.Api");
        if (!Directory.Exists(apiPath))
            throw new InvalidOperationException($"Resolved API path does not exist: {apiPath}");

        return apiPath;
    }

    private sealed class DesignTimeTenantContext : ITenantContext
    {
        public Guid? CurrentTenantId => null;
        public Guid? CurrentUserId => null;
    }
}
