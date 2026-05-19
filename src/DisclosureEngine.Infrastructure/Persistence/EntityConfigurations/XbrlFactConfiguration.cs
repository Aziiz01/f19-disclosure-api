using DisclosureEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DisclosureEngine.Infrastructure.Persistence.EntityConfigurations;

public sealed class XbrlFactConfiguration : IEntityTypeConfiguration<XbrlFact>
{
    public void Configure(EntityTypeBuilder<XbrlFact> b)
    {
        b.ToTable("XbrlFacts");

        b.HasKey(x => x.Id);

        b.Property(x => x.ReportId).IsRequired();
        b.Property(x => x.TenantId).IsRequired();

        b.Property(x => x.Concept)
            .IsRequired()
            .HasMaxLength(200);

        b.Property(x => x.Value)
            .HasColumnType("numeric(28,10)")
            .IsRequired();

        b.Property(x => x.Unit)
            .IsRequired()
            .HasMaxLength(50);

        b.Property(x => x.PeriodStart).IsRequired();
        b.Property(x => x.PeriodEnd).IsRequired();
        b.Property(x => x.Decimals).IsRequired();
        b.Property(x => x.CreatedAt).IsRequired();

        b.HasIndex(x => new { x.ReportId, x.Concept });
        b.HasIndex(x => x.TenantId);

        b.HasOne<Report>()
            .WithMany()
            .HasForeignKey(x => x.ReportId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
