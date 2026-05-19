using System.Reflection;
using DisclosureEngine.Application.Common.Interfaces;
using DisclosureEngine.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DisclosureEngine.Api.Controllers;

[ApiController]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    private readonly AppDbContext _db;

    public HealthController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Liveness probe. Always 200 — the status field is "healthy" or "degraded" based on DB reachability.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";

        bool dbReachable;
        try
        {
            dbReachable = await _db.Database.CanConnectAsync(ct);
        }
        catch
        {
            dbReachable = false;
        }

        return Ok(new
        {
            status   = dbReachable ? "healthy" : "degraded",
            version,
            database = dbReachable ? "connected" : "disconnected",
            timestampUtc = DateTime.UtcNow
        });
    }
}
