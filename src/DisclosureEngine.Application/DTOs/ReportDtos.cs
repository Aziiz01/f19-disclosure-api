using DisclosureEngine.Domain.Enums;

namespace DisclosureEngine.Application.DTOs;

public sealed record CreateReportRequest(string Title, int FiscalYear);

public sealed record UpdateReportRequest(string Title, int FiscalYear);

public sealed record ReportResponse(
    Guid Id,
    Guid TenantId,
    string Title,
    int FiscalYear,
    ReportStatus Status,
    DateTime CreatedAt,
    DateTime? SubmittedAt,
    DateTime? PublishedAt,
    Guid CreatedByUserId);
