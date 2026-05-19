using DisclosureEngine.Domain.Enums;

namespace DisclosureEngine.Domain.Entities;

public class Report
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public int FiscalYear { get; private set; }
    public ReportStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? SubmittedAt { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public Guid CreatedByUserId { get; private set; }

    private Report() { }

    public Report(string title, int fiscalYear, Guid tenantId, Guid createdByUserId)
        : this(Guid.NewGuid(), title, fiscalYear, tenantId, createdByUserId) { }

    public Report(Guid id, string title, int fiscalYear, Guid tenantId, Guid createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));
        if (fiscalYear <= 0)
            throw new ArgumentOutOfRangeException(nameof(fiscalYear), "FiscalYear must be positive.");
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));

        Id = id;
        Title = title.Trim();
        FiscalYear = fiscalYear;
        TenantId = tenantId;
        CreatedByUserId = createdByUserId;
        Status = ReportStatus.Draft;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(string title, int fiscalYear)
    {
        EnsureEditable();
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));
        if (fiscalYear <= 0)
            throw new ArgumentOutOfRangeException(nameof(fiscalYear), "FiscalYear must be positive.");

        Title = title.Trim();
        FiscalYear = fiscalYear;
    }

    /// <summary>Draft → Submitted. Throws if not currently Draft.</summary>
    public void Submit()
    {
        if (Status != ReportStatus.Draft)
            throw new InvalidOperationException(
                $"Cannot submit report in status '{Status}'. Only Draft reports can be submitted.");

        Status = ReportStatus.Submitted;
        SubmittedAt = DateTime.UtcNow;
    }

    /// <summary>Submitted → Published. Throws if not currently Submitted.</summary>
    public void Publish()
    {
        if (Status != ReportStatus.Submitted)
            throw new InvalidOperationException(
                $"Cannot publish report in status '{Status}'. Only Submitted reports can be published.");

        Status = ReportStatus.Published;
        PublishedAt = DateTime.UtcNow;
    }

    /// <summary>Throws if not Draft. Every edit path calls this, so no future method can forget the lock.</summary>
    public void EnsureEditable()
    {
        if (Status != ReportStatus.Draft)
            throw new InvalidOperationException(
                $"Report cannot be modified in status '{Status}'. Only Draft reports are editable.");
    }
}
