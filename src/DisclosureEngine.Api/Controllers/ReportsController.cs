using System.Globalization;
using DisclosureEngine.Application.Common.Interfaces;
using DisclosureEngine.Application.DTOs;
using DisclosureEngine.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DisclosureEngine.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/reports")]
public sealed class ReportsController : ControllerBase
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public ReportsController(IAppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    /// <summary>Lists reports for the caller's tenant. Empty list (not 404) if none exist.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ReportResponse>>> List(CancellationToken ct)
    {
        if (_tenantContext.CurrentTenantId is null)
            return BadRequest(new { error = "X-Tenant-Id header is required." });

        var reports = await _db.Reports
            .AsNoTracking()
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => Map(r))
            .ToListAsync(ct);

        return Ok(reports);
    }

    /// <summary>Returns one report by id. 404 if not found or belongs to a different tenant.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ReportResponse>> GetById(Guid id, CancellationToken ct)
    {
        if (_tenantContext.CurrentTenantId is null)
            return BadRequest(new { error = "X-Tenant-Id header is required." });

        var report = await _db.Reports
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (report is null)
            throw new KeyNotFoundException($"Report '{id}' not found.");

        return Ok(Map(report));
    }

    /// <summary>Creates a Draft report. 201 with Location; 400 if body is invalid.</summary>
    [HttpPost]
    public async Task<ActionResult<ReportResponse>> Create(
        [FromBody] CreateReportRequest request,
        CancellationToken ct)
    {
        if (_tenantContext.CurrentTenantId is null)
            return BadRequest(new { error = "X-Tenant-Id header is required." });
        if (request is null || string.IsNullOrWhiteSpace(request.Title) || request.FiscalYear <= 0)
            return BadRequest(new { error = "Title and a positive FiscalYear are required." });

        var report = new Report(
            title: request.Title,
            fiscalYear: request.FiscalYear,
            tenantId: _tenantContext.CurrentTenantId.Value,
            createdByUserId: _tenantContext.CurrentUserId ?? Guid.Empty);

        _db.Reports.Add(report);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = report.Id }, Map(report));
    }

    /// <summary>Updates title and fiscal year. 404 if not found; 409 if no longer Draft.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ReportResponse>> Update(
        Guid id,
        [FromBody] UpdateReportRequest request,
        CancellationToken ct)
    {
        if (_tenantContext.CurrentTenantId is null)
            return BadRequest(new { error = "X-Tenant-Id header is required." });
        if (request is null)
            return BadRequest(new { error = "Body is required." });

        var report = await _db.Reports.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new KeyNotFoundException($"Report '{id}' not found.");

        report.Update(request.Title, request.FiscalYear);
        await _db.SaveChangesAsync(ct);

        return Ok(Map(report));
    }

    /// <summary>Draft → Submitted. 404 if not found; 409 if wrong status.</summary>
    [HttpPost("{id:guid}/submit")]
    public async Task<ActionResult<ReportResponse>> Submit(Guid id, CancellationToken ct)
    {
        if (_tenantContext.CurrentTenantId is null)
            return BadRequest(new { error = "X-Tenant-Id header is required." });

        var report = await _db.Reports.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new KeyNotFoundException($"Report '{id}' not found.");

        report.Submit();
        await _db.SaveChangesAsync(ct);

        return Ok(Map(report));
    }

    /// <summary>Submitted → Published. 404 if not found; 409 if wrong status.</summary>
    [HttpPost("{id:guid}/publish")]
    public async Task<ActionResult<ReportResponse>> Publish(Guid id, CancellationToken ct)
    {
        if (_tenantContext.CurrentTenantId is null)
            return BadRequest(new { error = "X-Tenant-Id header is required." });

        var report = await _db.Reports.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new KeyNotFoundException($"Report '{id}' not found.");

        report.Publish();
        await _db.SaveChangesAsync(ct);

        return Ok(Map(report));
    }

    /// <summary>Deletes a Draft report. 204 on success; 404 if not found; 409 if not Draft.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        if (_tenantContext.CurrentTenantId is null)
            return BadRequest(new { error = "X-Tenant-Id header is required." });

        var report = await _db.Reports.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new KeyNotFoundException($"Report '{id}' not found.");

        report.EnsureEditable();
        _db.Reports.Remove(report);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }

    /// <summary>
    /// Parses an XBRL instance document and persists one XbrlFact row per extracted fact.
    /// 201 with parse summary; 400 missing/invalid file; 404 not found; 409 not Draft; 413 over 10 MB.
    /// </summary>
    [HttpPost("{id:guid}/xbrl/upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxXbrlBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxXbrlBytes)]
    public async Task<IActionResult> UploadXbrl(
        Guid id,
        IFormFile file,
        [FromServices] IXbrlParser parser,
        CancellationToken ct)
    {
        if (_tenantContext.CurrentTenantId is null) return Unauthorized();
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "File is required." });
        if (file.Length > MaxXbrlBytes)
            return StatusCode(StatusCodes.Status413PayloadTooLarge,
                new { error = $"File exceeds the {MaxXbrlBytes / (1024 * 1024)} MB limit." });

        var report = await _db.Reports.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new KeyNotFoundException($"Report '{id}' not found.");

        report.EnsureEditable(); // 409 via ExceptionHandlingMiddleware if not Draft

        XbrlParseResult parseResult;
        await using (var stream = file.OpenReadStream())
        {
            parseResult = await parser.ParseAsync(stream, ct);
        }

        var contextIndex = parseResult.Contexts.ToDictionary(c => c.Id, StringComparer.Ordinal);
        var unitIndex    = parseResult.Units.ToDictionary(u => u.Id,    StringComparer.Ordinal);
        var tenantId     = _tenantContext.CurrentTenantId.Value;

        var skipReasons = new List<string>();
        var created = 0;

        foreach (var pf in parseResult.Facts)
        {
            if (!contextIndex.TryGetValue(pf.ContextRef, out var ctx))
            {
                skipReasons.Add($"Skipped '{pf.Concept}': unknown contextRef '{pf.ContextRef}'.");
                continue;
            }

            if (pf.UnitRef is null || !unitIndex.TryGetValue(pf.UnitRef, out var unit))
            {
                skipReasons.Add($"Skipped '{pf.Concept}': unknown or missing unitRef '{pf.UnitRef ?? "(null)"}'.");
                continue;
            }

            if (!decimal.TryParse(pf.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
            {
                skipReasons.Add($"Skipped '{pf.Concept}': could not parse value '{pf.Value}' as decimal.");
                continue;
            }

            var periodStart = ctx.StartDate ?? ctx.Instant;
            var periodEnd   = ctx.EndDate   ?? ctx.Instant;
            if (periodStart is null || periodEnd is null)
            {
                skipReasons.Add($"Skipped '{pf.Concept}': context '{ctx.Id}' has no resolvable period.");
                continue;
            }

            var entity = new XbrlFact(
                reportId:    report.Id,
                tenantId:    tenantId,
                concept:     pf.Concept,
                value:       value,
                unit:        unit.Measure,
                periodStart: periodStart.Value,
                periodEnd:   periodEnd.Value,
                decimals:    pf.Decimals ?? 0);
            _db.XbrlFacts.Add(entity);
            created++;
        }

        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = report.Id }, new
        {
            reportId         = report.Id,
            totalFacts       = parseResult.TotalFacts,
            uniqueConcepts   = parseResult.UniqueConcepts,
            factsCreated     = created,
            periodStart      = parseResult.PeriodStart,
            periodEnd        = parseResult.PeriodEnd,
            contexts         = parseResult.Contexts.Count,
            units            = parseResult.Units.Count,
            validationErrors = parseResult.ValidationErrors,
            skipped          = skipReasons
        });
    }

    private const int MaxXbrlBytes = 10 * 1024 * 1024;

    private const int MaxAttachmentBytes = 50 * 1024 * 1024;

    private static readonly HashSet<string> AllowedAttachmentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    };

    /// <summary>
    /// Uploads a PDF/XLSX/DOCX to a Draft report. 201 with metadata; 404 not found;
    /// 409 not Draft; 413 over 50 MB; 415 wrong content-type.
    /// </summary>
    [HttpPost("{id:guid}/attachments")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxAttachmentBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxAttachmentBytes)]
    public async Task<ActionResult<AttachmentResponse>> UploadAttachment(
        Guid id,
        IFormFile file,
        [FromServices] IFileStorage fileStorage,
        CancellationToken ct)
    {
        if (_tenantContext.CurrentTenantId is null) return Unauthorized();
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "File is required." });
        if (file.Length > MaxAttachmentBytes)
            return StatusCode(StatusCodes.Status413PayloadTooLarge,
                new { error = $"File exceeds the {MaxAttachmentBytes / (1024 * 1024)} MB limit." });
        if (!AllowedAttachmentTypes.Contains(file.ContentType))
            return StatusCode(StatusCodes.Status415UnsupportedMediaType,
                new { error = "Allowed content types: application/pdf, .xlsx, .docx." });

        var report = await _db.Reports.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new KeyNotFoundException($"Report '{id}' not found.");
        report.EnsureEditable();

        string storageKey;
        await using (var stream = file.OpenReadStream())
        {
            storageKey = await fileStorage.UploadAsync(stream, file.FileName, file.ContentType, ct);
        }

        var attachment = new Attachment(
            reportId:    report.Id,
            tenantId:    _tenantContext.CurrentTenantId.Value,
            fileName:    file.FileName,
            contentType: file.ContentType,
            storageKey:  storageKey,
            sizeBytes:   file.Length);

        _db.Attachments.Add(attachment);
        await _db.SaveChangesAsync(ct);

        var response = new AttachmentResponse(
            attachment.Id, attachment.ReportId, attachment.FileName,
            attachment.ContentType, attachment.SizeBytes, attachment.UploadedAt);

        return CreatedAtAction("GetById", "Attachments", new { id = attachment.Id }, response);
    }

    /// <summary>Lists attachment metadata for the report. 404 if the report isn't in the caller's tenant.</summary>
    [HttpGet("{id:guid}/attachments")]
    public async Task<ActionResult<IReadOnlyList<AttachmentResponse>>> ListAttachments(
        Guid id,
        CancellationToken ct)
    {
        if (_tenantContext.CurrentTenantId is null) return Unauthorized();

        var reportExists = await _db.Reports.AnyAsync(r => r.Id == id, ct);
        if (!reportExists) throw new KeyNotFoundException($"Report '{id}' not found.");

        var attachments = await _db.Attachments
            .AsNoTracking()
            .Where(a => a.ReportId == id)
            .OrderByDescending(a => a.UploadedAt)
            .Select(a => new AttachmentResponse(
                a.Id, a.ReportId, a.FileName, a.ContentType, a.SizeBytes, a.UploadedAt))
            .ToListAsync(ct);

        return Ok(attachments);
    }

    private static ReportResponse Map(Report r) => new(
        r.Id,
        r.TenantId,
        r.Title,
        r.FiscalYear,
        r.Status,
        r.CreatedAt,
        r.SubmittedAt,
        r.PublishedAt,
        r.CreatedByUserId);
}
