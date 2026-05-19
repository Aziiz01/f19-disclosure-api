using DisclosureEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DisclosureEngine.Infrastructure.Persistence.EntityConfigurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("Users");

        b.HasKey(u => u.Id);

        b.Property(u => u.TenantId).IsRequired();

        b.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(320);

        b.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(500);

        b.Property(u => u.Role)
            .IsRequired()
            .HasConversion<int>();

        b.Property(u => u.CreatedAt).IsRequired();

        b.HasIndex(u => new { u.TenantId, u.Email }).IsUnique();

        b.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(u => u.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
