namespace DisclosureEngine.Domain.Entities;

public class XbrlFact
{
    public Guid Id { get; private set; }
    public Guid ReportId { get; private set; }
    public Guid TenantId { get; private set; }
    public string Concept { get; private set; } = string.Empty;
    public decimal Value { get; private set; }
    public string Unit { get; private set; } = string.Empty;
    public DateTime PeriodStart { get; private set; }
    public DateTime PeriodEnd { get; private set; }
    public int Decimals { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private XbrlFact() { }

    public XbrlFact(
        Guid reportId,
        Guid tenantId,
        string concept,
        decimal value,
        string unit,
        DateTime periodStart,
        DateTime periodEnd,
        int decimals)
        : this(Guid.NewGuid(), reportId, tenantId, concept, value, unit, periodStart, periodEnd, decimals) { }

    public XbrlFact(
        Guid id,
        Guid reportId,
        Guid tenantId,
        string concept,
        decimal value,
        string unit,
        DateTime periodStart,
        DateTime periodEnd,
        int decimals)
    {
        if (reportId == Guid.Empty)
            throw new ArgumentException("ReportId is required.", nameof(reportId));
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(concept))
            throw new ArgumentException("Concept is required.", nameof(concept));
        if (string.IsNullOrWhiteSpace(unit))
            throw new ArgumentException("Unit is required.", nameof(unit));
        if (periodEnd < periodStart)
            throw new ArgumentException("PeriodEnd must be on or after PeriodStart.", nameof(periodEnd));

        Id = id;
        ReportId = reportId;
        TenantId = tenantId;
        Concept = concept.Trim();
        Value = value;
        Unit = unit.Trim();
        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
        Decimals = decimals;
        CreatedAt = DateTime.UtcNow;
    }
}
