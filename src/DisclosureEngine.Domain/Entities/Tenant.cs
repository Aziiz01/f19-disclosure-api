namespace DisclosureEngine.Domain.Entities;

public class Tenant
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    private Tenant() { }

    public Tenant(string name) : this(Guid.NewGuid(), name) { }

    public Tenant(Guid id, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tenant name is required.", nameof(name));

        Id = id;
        Name = name.Trim();
        CreatedAt = DateTime.UtcNow;
    }
}
