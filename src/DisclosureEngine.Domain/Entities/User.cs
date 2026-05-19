using DisclosureEngine.Domain.Enums;

namespace DisclosureEngine.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private User() { }

    public User(Guid tenantId, string email, string passwordHash, UserRole role)
        : this(Guid.NewGuid(), tenantId, email, passwordHash, role) { }

    public User(Guid id, Guid tenantId, string email, string passwordHash, UserRole role)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));
        if (passwordHash is null)
            throw new ArgumentNullException(nameof(passwordHash));

        Id = id;
        TenantId = tenantId;
        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        Role = role;
        CreatedAt = DateTime.UtcNow;
    }
}
