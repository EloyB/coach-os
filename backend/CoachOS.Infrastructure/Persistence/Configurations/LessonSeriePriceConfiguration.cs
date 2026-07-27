using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachOS.Infrastructure.Persistence.Configurations;

public class LessonSeriePriceConfiguration : IEntityTypeConfiguration<LessonSeriePrice>
{
    public void Configure(EntityTypeBuilder<LessonSeriePrice> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Label)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(p => p.Description)
            .HasMaxLength(500);

        builder.Property(p => p.Mode)
            .HasDefaultValue(PricingMode.GroupSize)
            .IsRequired();

        builder.Property(p => p.TotalPrice)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(p => p.Category)
            .IsRequired(false);

        builder.Property(p => p.GroupSize)
            .IsRequired(false);

        builder.Property(p => p.ReusableKey)
            .HasMaxLength(120);

        builder.HasOne(p => p.Organization)
            .WithMany()
            .HasForeignKey(p => p.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.LessonSerie)
            .WithMany(ls => ls.Prices)
            .HasForeignKey(p => p.LessonSerieId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.OrganizationId);
        builder.HasIndex(p => new { p.LessonSerieId, p.Mode, p.GroupSize, p.Category });
        builder.HasIndex(p => new { p.OrganizationId, p.ReusableKey });
    }
}
