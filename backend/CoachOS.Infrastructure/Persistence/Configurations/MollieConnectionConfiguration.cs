using CoachOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachOS.Infrastructure.Persistence.Configurations;

public class MollieConnectionConfiguration : IEntityTypeConfiguration<MollieConnection>
{
    public void Configure(EntityTypeBuilder<MollieConnection> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.MollieOrganizationId)
            .IsRequired()
            .HasMaxLength(100);

        // Token strings worden via DataProtection beschermd; ruime kolom om
        // toekomstige rotaties / langere payloads op te vangen.
        builder.Property(c => c.AccessTokenEncrypted)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(c => c.RefreshTokenEncrypted)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(c => c.MollieOrganizationName)
            .HasMaxLength(200);

        builder.HasOne(c => c.Organization)
            .WithMany()
            .HasForeignKey(c => c.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.OrganizationId).IsUnique();
    }
}
