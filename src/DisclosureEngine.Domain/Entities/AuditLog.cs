namespace DisclosureEngine.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; private set; }
    public Guid? TenantId { get; private set; }
    public Guid? UserId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public DateTime TimestampUtc { get; private set; }
    public string Details { get; private set; } = string.Empty;

    private AuditLog() { }

    public AuditLog(
        Guid? tenantId,
        Guid? userId,
        string action,
        string entityType,
        Guid entityId,
        string details)
    {
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action is required.", nameof(action));
        if (string.IsNullOrWhiteSpace(entityType))
            throw new ArgumentException("EntityType is required.", nameof(entityType));

        Id = Guid.NewGuid();
        TenantId = tenantId;
        UserId = userId;
        Action = action;
        EntityType = entityType;
        EntityId = entityId;
        Details = details ?? string.Empty;
        TimestampUtc = DateTime.UtcNow;
    }
}
