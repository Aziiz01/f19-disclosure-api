using DisclosureEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DisclosureEngine.Infrastructure.Persistence.EntityConfigurations;

public sealed class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> b)
    {
        b.ToTable("Attachments");

        b.HasKey(a => a.Id);

        b.Property(a => a.ReportId).IsRequired();
        b.Property(a => a.TenantId).IsRequired();

        b.Property(a => a.FileName)
            .IsRequired()
            .HasMaxLength(500);

        b.Property(a => a.ContentType)
            .IsRequired()
            .HasMaxLength(200);

        b.Property(a => a.StorageKey)
            .IsRequired()
            .HasMaxLength(500);

        b.Property(a => a.SizeBytes).IsRequired();
        b.Property(a => a.UploadedAt).IsRequired();

        b.HasIndex(a => a.ReportId);
        b.HasIndex(a => a.TenantId);

        b.HasOne<Report>()
            .WithMany()
            .HasForeignKey(a => a.ReportId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(a => a.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
