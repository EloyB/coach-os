using CoachOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachOS.Infrastructure.Persistence.Configurations;

public class CampPriceConfiguration : IEntityTypeConfiguration<CampPrice>
{
    public void Configure(EntityTypeBuilder<CampPrice> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Label).HasMaxLength(120).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(500);
        builder.Property(p => p.Amount).HasPrecision(10, 2).IsRequired();

        builder.HasOne(p => p.Organization)
            .WithMany()
            .HasForeignKey(p => p.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Camp)
            .WithMany(c => c.Prices)
            .HasForeignKey(p => p.CampId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.OrganizationId);
        builder.HasIndex(p => new { p.CampId, p.SortOrder });
    }
}
