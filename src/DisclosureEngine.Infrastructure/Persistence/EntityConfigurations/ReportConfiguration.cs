using DisclosureEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DisclosureEngine.Infrastructure.Persistence.EntityConfigurations;

public sealed class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> b)
    {
        b.ToTable("Reports");

        b.HasKey(r => r.Id);

        b.Property(r => r.TenantId).IsRequired();

        b.Property(r => r.Title)
            .IsRequired()
            .HasMaxLength(500);

        b.Property(r => r.FiscalYear).IsRequired();

        b.Property(r => r.Status)
            .IsRequired()
            .HasConversion<int>();

        b.Property(r => r.CreatedAt).IsRequired();
        b.Property(r => r.SubmittedAt);
        b.Property(r => r.PublishedAt);
        b.Property(r => r.CreatedByUserId).IsRequired();

        b.HasIndex(r => new { r.TenantId, r.Status });
        b.HasIndex(r => new { r.TenantId, r.FiscalYear });

        b.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(r => r.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
