using CoachOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachOS.Infrastructure.Persistence.Configurations;

public class CampPackageConfiguration : IEntityTypeConfiguration<CampPackage>
{
    public void Configure(EntityTypeBuilder<CampPackage> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).HasMaxLength(120).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(500);
        builder.Property(p => p.Price).HasPrecision(10, 2).IsRequired();

        builder.HasOne(p => p.Camp)
            .WithMany(c => c.Packages)
            .HasForeignKey(p => p.CampId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.OrganizationId);
        builder.HasIndex(p => new { p.CampId, p.SortOrder });
    }
}
