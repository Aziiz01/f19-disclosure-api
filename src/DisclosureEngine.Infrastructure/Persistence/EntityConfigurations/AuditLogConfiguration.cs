using DisclosureEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DisclosureEngine.Infrastructure.Persistence.EntityConfigurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("AuditLogs");

        b.HasKey(a => a.Id);

        b.Property(a => a.TenantId);
        b.Property(a => a.UserId);

        b.Property(a => a.Action)
            .IsRequired()
            .HasMaxLength(200);

        b.Property(a => a.EntityType)
            .IsRequired()
            .HasMaxLength(200);

        b.Property(a => a.EntityId).IsRequired();
        b.Property(a => a.TimestampUtc).IsRequired();

        b.Property(a => a.Details)
            .IsRequired()
            .HasColumnType("text");

        b.HasIndex(a => new { a.TenantId, a.TimestampUtc });
        b.HasIndex(a => new { a.EntityType, a.EntityId });
    }
}
