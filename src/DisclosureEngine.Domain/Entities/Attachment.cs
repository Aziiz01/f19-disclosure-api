namespace DisclosureEngine.Domain.Entities;

public class Attachment
{
    public Guid Id { get; private set; }
    public Guid ReportId { get; private set; }
    public Guid TenantId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public string StorageKey { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public DateTime UploadedAt { get; private set; }

    private Attachment() { }

    public Attachment(
        Guid reportId,
        Guid tenantId,
        string fileName,
        string contentType,
        string storageKey,
        long sizeBytes)
        : this(Guid.NewGuid(), reportId, tenantId, fileName, contentType, storageKey, sizeBytes) { }

    public Attachment(
        Guid id,
        Guid reportId,
        Guid tenantId,
        string fileName,
        string contentType,
        string storageKey,
        long sizeBytes)
    {
        if (reportId == Guid.Empty)
            throw new ArgumentException("ReportId is required.", nameof(reportId));
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("FileName is required.", nameof(fileName));
        if (string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentException("ContentType is required.", nameof(contentType));
        if (string.IsNullOrWhiteSpace(storageKey))
            throw new ArgumentException("StorageKey is required.", nameof(storageKey));
        if (sizeBytes < 0)
            throw new ArgumentOutOfRangeException(nameof(sizeBytes), "SizeBytes cannot be negative.");

        Id = id;
        ReportId = reportId;
        TenantId = tenantId;
        FileName = fileName.Trim();
        ContentType = contentType.Trim();
        StorageKey = storageKey;
        SizeBytes = sizeBytes;
        UploadedAt = DateTime.UtcNow;
    }
}
