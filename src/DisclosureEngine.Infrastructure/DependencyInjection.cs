using DisclosureEngine.Application.Common.Interfaces;
using DisclosureEngine.Infrastructure.Identity;
using DisclosureEngine.Infrastructure.Interceptors;
using DisclosureEngine.Infrastructure.Persistence;
using DisclosureEngine.Infrastructure.Storage;
using DisclosureEngine.Infrastructure.Xbrl;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DisclosureEngine.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:Default is not configured. " +
                "Set via 'dotnet user-secrets set' on the API project.");

        services.AddScoped<AuditingInterceptor>();

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var interceptor = sp.GetRequiredService<AuditingInterceptor>();
            options.UseNpgsql(connectionString);
            options.AddInterceptors(interceptor);
        });

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        // Identity service registration lives in Api/Program.cs because AddIdentity<>
        // is in the ASP.NET Core shared framework, not in this class library. This file
        // owns infrastructure data services (DbContext, interceptor, file storage) only.

        services.AddScoped<IJwtTokenService, JwtTokenService>();

        services.AddSingleton<IXbrlParser, XbrlParser>();

        // IFileStorage: MinIO when Minio:Endpoint is configured, otherwise the
        // Day 1 in-memory implementation so tests and offline dev still work.
        if (!string.IsNullOrWhiteSpace(configuration["Minio:Endpoint"]))
        {
            services.AddSingleton<IFileStorage, MinioFileStorage>();
        }
        else
        {
            services.AddSingleton<IFileStorage, InMemoryFileStorage>();
        }

        // TODO: AzureBlobFileStorage implementation for Production environment.

        return services;
    }
}
