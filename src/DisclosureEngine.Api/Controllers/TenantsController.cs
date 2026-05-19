using DisclosureEngine.Application.Common.Interfaces;
using DisclosureEngine.Application.DTOs;
using DisclosureEngine.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DisclosureEngine.Api.Controllers;

[ApiController]
[Route("api/tenants")]
public sealed class TenantsController : ControllerBase
{
    private readonly IAppDbContext _db;

    public TenantsController(IAppDbContext db)
    {
        _db = db;
    }

    /// <summary>Lists every tenant. Not tenant-scoped — <c>Tenant</c> has no query filter.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TenantResponse>>> List(CancellationToken ct)
    {
        var tenants = await _db.Tenants
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new TenantResponse(t.Id, t.Name, t.CreatedAt))
            .ToListAsync(ct);

        return Ok(tenants);
    }

    /// <summary>Returns the tenant with the given id. 404 if not found.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TenantResponse>> GetById(Guid id, CancellationToken ct)
    {
        var tenant = await _db.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (tenant is null)
            throw new KeyNotFoundException($"Tenant '{id}' not found.");

        return Ok(new TenantResponse(tenant.Id, tenant.Name, tenant.CreatedAt));
    }

    /// <summary>Creates a tenant. 201 with Location; 400 if Name is missing.</summary>
    [HttpPost]
    public async Task<ActionResult<TenantResponse>> Create(
        [FromBody] CreateTenantRequest request,
        CancellationToken ct)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "Name is required." });

        var tenant = new Tenant(request.Name);
        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync(ct);

        var response = new TenantResponse(tenant.Id, tenant.Name, tenant.CreatedAt);
        return CreatedAtAction(nameof(GetById), new { id = tenant.Id }, response);
    }
}
