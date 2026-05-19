using DisclosureEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DisclosureEngine.Infrastructure.Persistence.EntityConfigurations;

public sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> b)
    {
        b.ToTable("Tenants");

        b.HasKey(t => t.Id);

        b.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(200);

        b.Property(t => t.CreatedAt).IsRequired();

        b.HasIndex(t => t.Name).IsUnique();
    }
}
