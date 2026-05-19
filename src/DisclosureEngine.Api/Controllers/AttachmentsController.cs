using DisclosureEngine.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DisclosureEngine.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/attachments")]
public sealed class AttachmentsController : ControllerBase
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly IFileStorage _fileStorage;

    public AttachmentsController(
        IAppDbContext db,
        ITenantContext tenantContext,
        IFileStorage fileStorage)
    {
        _db = db;
        _tenantContext = tenantContext;
        _fileStorage = fileStorage;
    }

    /// <summary>Streams the attachment back. Returns 404 for missing or cross-tenant.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        if (_tenantContext.CurrentTenantId is null) return Unauthorized();

        var attachment = await _db.Attachments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (attachment is null)
            throw new KeyNotFoundException($"Attachment '{id}' not found.");

        var stream = await _fileStorage.DownloadAsync(attachment.StorageKey, ct);
        return File(stream, attachment.ContentType, attachment.FileName);
    }

    /// <summary>Deletes an attachment. 204 on success; 404 if not found; 409 if parent report isn't Draft.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        if (_tenantContext.CurrentTenantId is null) return Unauthorized();

        var attachment = await _db.Attachments.FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new KeyNotFoundException($"Attachment '{id}' not found.");

        var report = await _db.Reports.FirstOrDefaultAsync(r => r.Id == attachment.ReportId, ct)
            ?? throw new KeyNotFoundException($"Parent report '{attachment.ReportId}' not found.");

        report.EnsureEditable(); // 409 via middleware if not Draft

        _db.Attachments.Remove(attachment);
        await _db.SaveChangesAsync(ct);

        // Best-effort: remove the blob after the metadata row is committed.
        await _fileStorage.DeleteAsync(attachment.StorageKey, ct);

        return NoContent();
    }
}
